using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ItemText
    {
        internal const string NexusPatchOwner = "com.supernova.valheimspawner.InputGuard";
        internal static bool OwnsName(ItemDrop.ItemData item) => PersonalizedText.Owns(item?.m_customData, false);
        internal static bool OwnsDescription(ItemDrop.ItemData item) => PersonalizedText.Owns(item?.m_customData, true);
        internal static string GetName(ItemDrop.ItemData item) =>
            PersonalizedText.Resolve(item?.m_customData, L.OriginalName(item), false);
        internal static string GetDescription(ItemDrop.ItemData item) =>
            PersonalizedText.Resolve(item?.m_customData, L.OriginalDescription(item), true);
        internal static bool NameChanges(ItemDrop.ItemData item, string text, bool reset) =>
            PersonalizedText.Changes(item?.m_customData, L.OriginalName(item), text, reset, false);
        internal static bool DescriptionChanges(ItemDrop.ItemData item, string text, bool reset) =>
            PersonalizedText.Changes(item?.m_customData, L.OriginalDescription(item), text, reset, true);

        internal static bool Validate(string input, int max, bool allowNewlines, out string error)
        {
            error = null;
            if (input == null || input.Length > max)
            { error = L.T("Text is too long.", "Текст слишком длинный."); return false; }
            foreach (char c in input)
                if (c == '<' || c == '>' || char.IsControl(c) && !(allowNewlines && c == '\n'))
                {
                    error = L.T("Control characters and rich-text tags are not allowed.", "Управляющие символы и теги форматирования запрещены.");
                    return false;
                }
            return true;
        }

        internal static void Apply(ItemDrop.ItemData item, string name, string description,
            bool changeName, bool changeDescription, bool resetName, bool resetDescription)
        {
            if (item.m_customData == null) item.m_customData = new Dictionary<string, string>();
            if (changeName) PersonalizedText.Apply(item.m_customData, L.OriginalName(item), name, resetName, false);
            if (changeDescription) PersonalizedText.Apply(item.m_customData, L.OriginalDescription(item), description, resetDescription, true);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "GetHoverName")]
    internal static class GroundItemHoverNamePatch
    {
        [HarmonyPriority(Priority.Last), HarmonyAfter(ItemText.NexusPatchOwner)]
        private static void Postfix(ItemDrop __instance, ref string __result)
        {
            if (__instance != null && ItemText.OwnsName(__instance.m_itemData))
                __result = ItemText.GetName(__instance.m_itemData);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "GetHoverText")]
    internal static class GroundItemHoverTextPatch
    {
        [HarmonyPriority(Priority.Last), HarmonyAfter(ItemText.NexusPatchOwner)]
        private static void Postfix(ItemDrop __instance, ref string __result)
        {
            var item = __instance?.m_itemData;
            if (!ItemText.OwnsName(item) || string.IsNullOrEmpty(__result)) return;
            __result = PersonalizedText.ReplaceHoverName(__result, ItemText.GetName(item), ItemText.GetName(item),
                PersonalizedText.Read(item.m_customData, PersonalizedText.NexusNameKey),
                L.OriginalName(item), item.m_shared?.m_name);
        }
    }

    [HarmonyPatch]
    internal static class ItemTooltipDescriptionPatch
    {
        private static bool s_warned;
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(ItemDrop.ItemData)).Where(m =>
                m.Name == "GetTooltip" && m.ReturnType == typeof(string) && m.IsStatic &&
                m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(ItemDrop.ItemData));

        [HarmonyPriority(Priority.Last), HarmonyAfter(ItemText.NexusPatchOwner)]
        private static void Postfix(object[] __args, ref string __result)
        {
            if (__args == null || __args.Length == 0 || !(__args[0] is ItemDrop.ItemData item) ||
                !ItemText.OwnsDescription(item) || string.IsNullOrEmpty(__result)) return;
            string replacement = ItemText.GetDescription(item);
            string updated = PersonalizedText.ReplaceDescription(__result, replacement,
                item.m_shared?.m_description, L.OriginalDescription(item),
                PersonalizedText.Read(item.m_customData, PersonalizedText.NexusDescriptionKey));
            if (!s_warned && updated == __result && !__result.StartsWith(replacement, StringComparison.Ordinal))
            {
                s_warned = true;
                NamekeeperPlugin.Log?.LogWarning("Namekeeper: unknown tooltip description layout; preserved other-mod text and stats.");
            }
            __result = updated;
        }
    }

    [HarmonyPatch]
    internal static class InventoryTooltipTitlePatch
    {
        private static bool s_warned, s_loggedUpdate;
        private static MethodBase TargetMethod()
        {
            // Confirmed in Valheim 1.0.20; UITooltip lives in assembly_guiutils.
            MethodInfo method = AccessTools.GetDeclaredMethods(typeof(InventoryGrid)).FirstOrDefault(m =>
                m.Name == "CreateItemTooltip" && !m.IsStatic && m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(ItemDrop.ItemData) &&
                m.GetParameters()[1].ParameterType.Name == "UITooltip");
            if (method == null) throw new MissingMethodException("InventoryGrid.CreateItemTooltip(ItemData, UITooltip)");
            NamekeeperPlugin.Log?.LogInfo("Namekeeper: patch inventory tooltip titles after CreateItemTooltip (UITooltip.Set)");
            return method;
        }

        [HarmonyPriority(Priority.Last), HarmonyAfter(ItemText.NexusPatchOwner)]
        private static void Postfix(object[] __args)
        {
            if (__args == null || __args.Length < 2 || !(__args[0] is ItemDrop.ItemData item) ||
                !ItemText.OwnsName(item)) return;
            try
            {
                TooltipTitleUpdater.SetTitle(__args[1], ItemText.GetName(item));
                if (!s_loggedUpdate)
                {
                    s_loggedUpdate = true;
                    NamekeeperPlugin.Log?.LogInfo("Namekeeper: customized InventoryGrid tooltip refreshed through UITooltip.Set.");
                }
            }
            catch (Exception ex)
            {
                if (s_warned) return;
                s_warned = true;
                NamekeeperPlugin.Log?.LogWarning("Namekeeper inventory tooltip refresh failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class BlockGameplayWhenShopOpen
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (__instance == Player.m_localPlayer && NamekeeperInputState.ShopOpen)
            { __result = false; return false; }
            return true;
        }
    }
}
