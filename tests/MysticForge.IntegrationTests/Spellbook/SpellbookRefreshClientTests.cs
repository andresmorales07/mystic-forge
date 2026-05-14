using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MysticForge.Application.Spellbook;
using MysticForge.CommanderSpellbook.Generated;
using MysticForge.Infrastructure.Spellbook;
using WireMock.RequestBuilders;
using WireMock.Server;
using WmResponse = WireMock.ResponseBuilders.Response;

namespace MysticForge.IntegrationTests.Spellbook;

/// <summary>Integration tests that stand up a WireMock server and exercise <see cref="SpellbookRefreshClient"/>.</summary>
public sealed class SpellbookRefreshClientTests : IDisposable
{
    private readonly WireMockServer _wm;

    public SpellbookRefreshClientTests()
    {
        _wm = WireMockServer.Start(new WireMock.Settings.WireMockServerSettings
        {
            StartAdminInterface = false,
        });
    }

    public void Dispose() => _wm.Dispose();

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    private SpellbookRefreshClient CreateSut(int perPageDelayMs = 0, int pageSize = 100)
    {
        var baseUrl = _wm.Url!.TrimEnd('/') + "/";
        var http    = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http);
        var kiota   = new SpellbookApiClient(adapter);

        var opts = Options.Create(new CommanderSpellbookOptions
        {
            BaseUrl                      = baseUrl,
            PageSize                     = pageSize,
            PerPageDelayMs               = perPageDelayMs,
            RefreshRequestTimeoutSeconds = 5,
        });

        return new SpellbookRefreshClient(kiota, opts);
    }

    private static string BuildVariantPage(string? nextUrl, params (string id, string identity)[] items)
    {
        var results = items.Select(i => new
        {
            id         = i.id,
            identity   = i.identity,
            status     = "OK",
            uses       = Array.Empty<object>(),
            produces   = Array.Empty<object>(),
            requires   = Array.Empty<object>(),
            spoiler    = false,
        });
        return JsonSerializer.Serialize(new
        {
            count   = items.Length,
            next    = nextUrl,
            previous = (string?)null,
            results,
        });
    }

    private static string BuildFeaturePage(string? nextUrl, params (int id, string name)[] items)
    {
        var results = items.Select(i => new
        {
            id     = i.id,
            name   = i.name,
            status = "S",
            uncountable = false,
        });
        return JsonSerializer.Serialize(new
        {
            count   = items.Length,
            next    = nextUrl,
            previous = (string?)null,
            results,
        });
    }

    private static string BuildTemplatePage(string? nextUrl, params (int id, string name)[] items)
    {
        var results = items.Select(i => new
        {
            id            = i.id,
            name          = i.name,
            scryfallQuery = (string?)null,
        });
        return JsonSerializer.Serialize(new
        {
            count   = items.Length,
            next    = nextUrl,
            previous = (string?)null,
            results,
        });
    }

    // -------------------------------------------------------------------------
    // Tests: variants
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Streams_variants_across_pages()
    {
        // Page 1 → 2 variants, next points to page 2
        // Page 2 → 1 variant,  next = null
        _wm.Given(Request.Create().WithPath("/variants/").WithParam("offset", "0").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildVariantPage(
                   nextUrl: "/variants/?limit=100&offset=100",
                   ("v-1", "WUBRG"), ("v-2", "C"))));

        _wm.Given(Request.Create().WithPath("/variants/").WithParam("offset", "100").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildVariantPage(
                   nextUrl: null,
                   ("v-3", "W"))));

        var sut    = CreateSut();
        var pages  = new List<IReadOnlyList<RawCombo>>();

        await foreach (var page in sut.StreamVariantsAsync(CancellationToken.None))
            pages.Add(page);

        pages.Should().HaveCount(2);
        pages[0].Should().HaveCount(2);
        pages[1].Should().HaveCount(1);
        pages.SelectMany(p => p).Select(c => c.Id)
             .Should().BeEquivalentTo(["v-1", "v-2", "v-3"]);
    }

    [Fact]
    public async Task Throws_SpellbookPageException_when_upstream_returns_500_persistently()
    {
        _wm.Given(Request.Create().WithPath("/variants/").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(500));

        var sut = CreateSut();

        var act = async () =>
        {
            await foreach (var _ in sut.StreamVariantsAsync(CancellationToken.None)) { }
        };

        await act.Should().ThrowAsync<SpellbookPageException>()
                 .WithMessage("*variants*");
    }

    // -------------------------------------------------------------------------
    // Tests: features
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Streams_features_paginated()
    {
        _wm.Given(Request.Create().WithPath("/features/").WithParam("offset", "0").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildFeaturePage(
                   nextUrl: "/features/?limit=100&offset=100",
                   (1, "Infinite Mana"), (2, "Draw Cards"))));

        _wm.Given(Request.Create().WithPath("/features/").WithParam("offset", "100").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildFeaturePage(
                   nextUrl: null,
                   (3, "Win the Game"))));

        var sut  = CreateSut();
        var all  = new List<RawFeature>();

        await foreach (var page in sut.StreamFeaturesAsync(CancellationToken.None))
            all.AddRange(page);

        all.Should().HaveCount(3);
        all.Select(f => f.Name)
           .Should().BeEquivalentTo(["Infinite Mana", "Draw Cards", "Win the Game"]);
    }

    // -------------------------------------------------------------------------
    // Tests: templates
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Streams_templates_paginated()
    {
        _wm.Given(Request.Create().WithPath("/templates/").WithParam("offset", "0").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildTemplatePage(
                   nextUrl: "/templates/?limit=100&offset=100",
                   (10, "Any land"), (11, "Any creature"))));

        _wm.Given(Request.Create().WithPath("/templates/").WithParam("offset", "100").UsingGet())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(BuildTemplatePage(
                   nextUrl: null,
                   (12, "Any artifact"))));

        var sut = CreateSut();
        var all = new List<RawTemplate>();

        await foreach (var page in sut.StreamTemplatesAsync(CancellationToken.None))
            all.AddRange(page);

        all.Should().HaveCount(3);
        all.Select(t => t.Name)
           .Should().BeEquivalentTo(["Any land", "Any creature", "Any artifact"]);
    }
}
