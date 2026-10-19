using System;
using Jotunn.Managers;

namespace SuperNova.ValheimNamekeeper
{
    internal static class L
    {
        internal static bool Ru => string.Equals(NamekeeperPlugin.Language.Value, "Russian", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(NamekeeperPlugin.Language.Value, "ru", StringComparison.OrdinalIgnoreCase);
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
