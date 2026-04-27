using System.Text.Json.Serialization;

namespace MysticForge.Infrastructure.Tagging;

internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")] public required string Model { get; init; }
    [JsonPropertyName("messages")] public required IList<MessageEnvelope> Messages { get; init; }
    [JsonPropertyName("response_format")] public required object ResponseFormat { get; init; }
    [JsonPropertyName("temperature")] public double Temperature { get; init; } = 0.0;
    [JsonPropertyName("max_tokens")] public int MaxTokens { get; init; } = 800;
}

internal sealed class MessageEnvelope
{
    [JsonPropertyName("role")] public required string Role { get; init; }
    [JsonPropertyName("content")] public required IList<ContentPart> Content { get; init; }
}

internal sealed class ContentPart
{
    [JsonPropertyName("type")] public required string Type { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("cache_control")] public CacheControl? CacheControl { get; init; }
}

internal sealed class CacheControl
{
    [JsonPropertyName("type")] public string Type { get; init; } = "ephemeral";
}

internal sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")] public List<Choice> Choices { get; init; } = [];
}

internal sealed class Choice
{
    [JsonPropertyName("message")] public required ChoiceMessage Message { get; init; }
}

internal sealed class ChoiceMessage
{
    [JsonPropertyName("content")] public required string Content { get; init; }
}
