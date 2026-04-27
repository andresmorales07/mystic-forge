using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MysticForge.Application.Tagging;

namespace MysticForge.Infrastructure.Tagging;

public sealed class OpenRouterTaggingClient : IOpenRouterTaggingClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly object JsonSchemaResponseFormat = new
    {
        type = "json_schema",
        json_schema = new
        {
            name = "card_tags",
            strict = true,
            schema = new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["roles"] = new
                    {
                        type = "array",
                        items = new { @enum = new[] { "ramp", "draw", "tutor", "removal", "counterspell", "wipe", "protection", "win_con", "stax", "lock_piece", "utility" } },
                    },
                    ["synergy_hook_paths"] = new { type = "array", items = new { type = "string" } },
                    ["mechanics"] = new { type = "array", items = new { type = "string" } },
                    ["tribal_interest"] = new { type = "array", items = new { type = "string" } },
                },
                required = new[] { "roles", "synergy_hook_paths", "mechanics", "tribal_interest" },
                additionalProperties = false,
            },
        },
    };

    private readonly HttpClient _http;
    private readonly IPromptBuilder _prompts;
    private readonly ILogger<OpenRouterTaggingClient> _log;
    private string _model = string.Empty;

    public OpenRouterTaggingClient(HttpClient http, IPromptBuilder prompts, ILogger<OpenRouterTaggingClient> log)
    {
        _http = http;
        _prompts = prompts;
        _log = log;
    }

    public string CurrentModelVersion => _model;

    public void Configure(string model, string apiKey)
    {
        _model = model;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    /// <summary>Test-only — skips DI plumbing.</summary>
    internal void ConfigureForTesting(string model, string apiKey) => Configure(model, apiKey);

    public async Task<RawTagSet> TagAsync(CardForTagging card, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_model)) throw new InvalidOperationException("OpenRouterTaggingClient.Configure was not called.");

        var preamble = _prompts.GetSystemPreamble();
        var userJson = JsonSerializer.Serialize(card, SerializerOptions);

        var request = new ChatCompletionRequest
        {
            Model = _model,
            Messages =
            [
                new MessageEnvelope
                {
                    Role = "system",
                    Content = [ new ContentPart { Type = "text", Text = preamble, CacheControl = new CacheControl() } ],
                },
                new MessageEnvelope
                {
                    Role = "user",
                    Content = [ new ContentPart { Type = "text", Text = userJson } ],
                },
            ],
            ResponseFormat = JsonSchemaResponseFormat,
        };

        using var response = await _http.PostAsJsonAsync("chat/completions", request, SerializerOptions, ct);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(SerializerOptions, ct);
        if (envelope is null)
        {
            _log.LogWarning("OpenRouter returned an empty response envelope for {Card}.", card.Name);
            throw new InvalidOperationException("OpenRouter returned an empty response.");
        }
        if (envelope.Choices.Count == 0)
        {
            _log.LogWarning("OpenRouter response had zero choices for {Card}.", card.Name);
            throw new InvalidOperationException("OpenRouter response had zero choices.");
        }

        var content = envelope.Choices[0].Message.Content;
        var tagSet = JsonSerializer.Deserialize<RawTagSet>(content, SerializerOptions);
        if (tagSet is null)
        {
            _log.LogWarning("LLM returned null tag set for {Card}; raw content: {Content}", card.Name, content);
            throw new InvalidOperationException("LLM returned null tag set.");
        }
        _log.LogDebug("Tagged {Card} with {RoleCount} roles, {HookCount} hooks via {Model}.", card.Name, tagSet.Roles.Count, tagSet.SynergyHookPaths.Count, _model);
        return tagSet;
    }
}
