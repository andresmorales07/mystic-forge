namespace MysticForge.Application.Tagging;

public interface IPromptBuilder
{
    /// <summary>
    /// The cacheable system preamble. Built once from TaxonomyCache; rebuilt only on cache reload.
    /// Sent with `cache_control: ephemeral` so OpenRouter passes it to Anthropic's prompt cache.
    /// </summary>
    string GetSystemPreamble();
}
