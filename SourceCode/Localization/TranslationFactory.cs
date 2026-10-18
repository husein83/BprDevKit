using System;
using System.Collections.Concurrent;
using BprDevKit.Config;
using BprDevKit.Utility;

namespace BprDevKit.Localization
{
    /// <summary>
    /// Thread-safe factory for providing translators and managing runtime language changes.
    /// </summary>
    public class TranslationFactory : ITranslationFactory
    {
        private readonly ConcurrentDictionary<Language, ITranslator> _translators = new();

        public event Action<Language>? LanguageChanged;

        public TranslationFactory()
        {
            Register(new EnglishTranslator());
            Register(new PersianTranslator());
        }

        public void Register(ITranslator translator)
        {
            ArgumentNullException.ThrowIfNull(translator);
            _translators[translator.Language] = translator;
        }

        public Language CurrentLanguage => ApplicationConfiguration.Config.Language;

        public ITranslator Current => GetTranslator(CurrentLanguage);

        public ITranslator GetTranslator(Language language)
        {
            if (_translators.TryGetValue(language, out var translator))
                return translator;

            // Fallback to English
            if (_translators.TryGetValue(Language.English, out var englishTranslator))
                return englishTranslator;

            return new EnglishTranslator();
        }

        public void SetLanguage(Language language)
        {
            if (ApplicationConfiguration.Config.Language == language)
                return;

            ApplicationConfiguration.Config.Language = language;
            try
            {
                ApplicationConfiguration.SaveChanges(ApplicationConfiguration.Config);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex);
            }

            LanguageChanged?.Invoke(language);
        }
    }
}
