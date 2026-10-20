using System;
using Jotunn.Managers;

namespace SuperNova.ValheimNamekeeper
{
    internal static class L
    {
        internal static bool Ru
        {
            get
            {
                string gameLanguage = null;
                try
                {
                    if (global::Localization.instance != null)
                        gameLanguage = global::Localization.instance.GetSelectedLanguage();
                }
                catch (Exception) { }
                return UiLanguagePolicy.UseRussian(NamekeeperPlugin.Language?.Value, gameLanguage);
            }
        }
        internal static string T(string en, string ru) => Ru ? ru : en;
        // Valheim 1.0 removed/renamed the old global Localization surface used by previous builds.
        // Jotunn's manager delegates unknown tokens to the active game's localization.
        internal static string Vanilla(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith("$", StringComparison.Ordinal)) return token ?? "";
            try { return LocalizationManager.Instance.TryTranslate(token) ?? token; }
            catch (Exception) { return token; }
        }
        internal static string Name(ItemDrop.ItemData item) => item == null ? "" : ItemText.GetName(item);
        internal static string OriginalName(ItemDrop.ItemData item) => item?.m_shared == null ? "" : Vanilla(item.m_shared.m_name);
        internal static string OriginalDescription(ItemDrop.ItemData item) => item?.m_shared == null ? "" : Vanilla(item.m_shared.m_description);
    }
}
