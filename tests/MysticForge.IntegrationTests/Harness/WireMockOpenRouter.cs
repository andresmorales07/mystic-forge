using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace MysticForge.IntegrationTests.Harness;

public sealed class WireMockOpenRouter : IDisposable
{
    private readonly WireMockServer _server;

    public WireMockOpenRouter()
    {
        _server = WireMockServer.Start(new WireMockServerSettings { StartAdminInterface = false });
    }

    public Uri BaseAddress => new(_server.Url!.TrimEnd('/') + "/");

    public void Reset() => _server.Reset();

    /// <summary>Stubs a successful 200 response with the given JSON as the assistant message body.</summary>
    public void StubChatCompletion(string contentJson)
    {
        var body = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = contentJson } } }
        });
        _server.Given(Request.Create().WithPath("/chat/completions").UsingPost())
               .RespondWith(Response.Create().WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json").WithBody(body));
    }

    /// <summary>Stubs failure on first N calls, then 200. Uses WireMock scenarios (state machine).</summary>
    public void StubFailingThenSuccess(int failuresBeforeSuccess, int errorStatus, string successContentJson)
    {
        const string scenarioName = "retry-scenario";
        const string successStateName = "succeeded";

        var successBody = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = successContentJson } } }
        });

        if (failuresBeforeSuccess <= 0)
        {
            StubChatCompletion(successContentJson);
            return;
        }

        // State machine: initial (no WhenStateIs) → "fail-1" → ... → "fail-N" → "succeeded"
        // Omitting WhenStateIs on the first stub matches the unset initial state.
        for (int i = 0; i < failuresBeforeSuccess; i++)
        {
            var nextState = i < failuresBeforeSuccess - 1 ? $"fail-{i + 1}" : successStateName;

            var stub = _server.Given(Request.Create().WithPath("/chat/completions").UsingPost())
                              .InScenario(scenarioName);

            // Only add WhenStateIs for stubs after the first one
            if (i > 0)
                stub = stub.WhenStateIs($"fail-{i}");

            stub.WillSetStateTo(nextState)
                .RespondWith(Response.Create().WithStatusCode(errorStatus));
        }

        // Success stub fires once state reaches successStateName
        _server.Given(Request.Create().WithPath("/chat/completions").UsingPost())
               .InScenario(scenarioName)
               .WhenStateIs(successStateName)
               .RespondWith(Response.Create().WithStatusCode(200)
                   .WithHeader("Content-Type", "application/json").WithBody(successBody));
    }

    public void Dispose() => _server.Dispose();
}
