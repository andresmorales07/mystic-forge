using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MysticForge.Application.Tagging;
using MysticForge.Infrastructure.Tagging;
using MysticForge.IntegrationTests.Harness;

namespace MysticForge.IntegrationTests.Tagging;

public sealed class OpenRouterTaggingClientTests : IDisposable
{
    private readonly WireMockOpenRouter _wm = new();
    private ServiceProvider? _sp;

    public void Dispose()
    {
        _sp?.Dispose();
        _wm.Dispose();
    }

    private OpenRouterTaggingClient CreateClient()
    {
        _sp?.Dispose();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<ITaxonomyCache>(_ =>
        {
            var c = new TaxonomyCache();
            c.LoadForTesting("v1", []);
            return c;
        });
        services.AddSingleton<IPromptBuilder, PromptBuilder>();

        services.AddHttpClient<IOpenRouterTaggingClient, OpenRouterTaggingClient>(http =>
        {
            http.BaseAddress = _wm.BaseAddress;
        }).AddStandardResilienceHandler();

        _sp = services.BuildServiceProvider();
        var typed = (OpenRouterTaggingClient)_sp.GetRequiredService<IOpenRouterTaggingClient>();
        typed.ConfigureForTesting(model: "test-haiku", apiKey: "test-key");
        return typed;
    }

    [Fact]
    public async Task ParsesValidJsonResponse_IntoRawTagSet()
    {
        _wm.StubChatCompletion("""
            {"roles":["ramp"],"synergy_hook_paths":["graveyard_value/reanimate"],"mechanics":["flashback"],"tribal_interest":[]}
            """);

        var client = CreateClient();
        var card = new CardForTagging("Sol Ring", "{1}", "Artifact", "{T}: Add {C}{C}.", null);

        var result = await client.TagAsync(card, default);

        result.Roles.Should().BeEquivalentTo(["ramp"]);
        result.SynergyHookPaths.Should().BeEquivalentTo(["graveyard_value/reanimate"]);
        result.Mechanics.Should().BeEquivalentTo(["flashback"]);
        result.TribalInterest.Should().BeEmpty();
    }

    [Fact]
    public async Task RetriesOn500_AndSucceedsOnSecondAttempt()
    {
        _wm.StubFailingThenSuccess(failuresBeforeSuccess: 1, errorStatus: 500, successContentJson: """
            {"roles":[],"synergy_hook_paths":[],"mechanics":[],"tribal_interest":[]}
            """);

        var client = CreateClient();
        var card = new CardForTagging("Vanilla", "{1}", "Creature", "", null);

        var result = await client.TagAsync(card, default);
        result.Roles.Should().BeEmpty();
    }
}
