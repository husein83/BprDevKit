using System;
using BprDevKit.Utility;

namespace BprDevKit.Localization
{
    /// <summary>
    /// Factory for obtaining and managing translators across languages.
    /// </summary>
    public interface ITranslationFactory
    {
        ITranslator Current { get; }

        Language CurrentLanguage { get; }

        ITranslator GetTranslator(Language language);

        void SetLanguage(Language language);

        event Action<Language>? LanguageChanged;
    }
}
