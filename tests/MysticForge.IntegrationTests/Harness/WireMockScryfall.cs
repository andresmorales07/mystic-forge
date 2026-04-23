using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace MysticForge.IntegrationTests.Harness;

public sealed class WireMockScryfall : IDisposable
{
    private readonly WireMockServer _server;

    public WireMockScryfall()
    {
        _server = WireMockServer.Start(new WireMockServerSettings { StartAdminInterface = false });
    }

    public Uri BaseAddress => new(_server.Url!.TrimEnd('/') + "/");

    public int GetDownloadCallCount()
        => _server.LogEntries.Count(l => l.RequestMessage?.Path?.EndsWith("/bulk.json", StringComparison.OrdinalIgnoreCase) ?? false);

    public void GivenBulkMetadata(DateTimeOffset updatedAt, string downloadPath = "/bulk.json")
    {
        var body = JsonSerializer.Serialize(new
        {
            @object = "list",
            has_more = false,
            data = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "default_cards",
                    updated_at = updatedAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    download_uri = _server.Url!.TrimEnd('/') + downloadPath,
                },
            },
        });

        _server.Given(Request.Create().WithPath("/bulk-data").UsingGet())
               .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(body));
    }

    public void GivenBulkFile(string downloadPath, IEnumerable<string> rawCardJsons)
    {
        var arrayBody = "[" + string.Join(",", rawCardJsons) + "]";
        GivenBulkFileRaw(downloadPath, arrayBody);
    }

    public void GivenBulkFileRaw(string downloadPath, string arrayBody)
    {
        _server.Given(Request.Create().WithPath(downloadPath).UsingGet())
               .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(arrayBody));
    }

    public void Dispose() => _server.Dispose();
}
