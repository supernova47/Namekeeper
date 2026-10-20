using System;

namespace SuperNova.ValheimNamekeeper
{
    internal static class UiLanguagePolicy
    {
        internal static bool UseRussian(string overrideLanguage, string gameLanguage)
        {
            if (string.Equals(overrideLanguage, "Russian", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(overrideLanguage, "ru", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(overrideLanguage, "English", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(overrideLanguage, "en", StringComparison.OrdinalIgnoreCase)) return false;
            return DialogueLines.UseRussian(gameLanguage);
        }
    }
}
