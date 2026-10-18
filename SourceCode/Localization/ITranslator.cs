using BprDevKit.Utility;

namespace BprDevKit.Localization
{
    /// <summary>
    /// Represents a localized translator for a specific language.
    /// </summary>
    public interface ITranslator
    {
        Language Language { get; }

        string LanguageCode { get; }

        bool IsRtl { get; }

        string this[string key] { get; }

        string Get(string key, params object[] args);
    }
}
