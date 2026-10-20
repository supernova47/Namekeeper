using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ShopSanctuary
    {
        internal const float Radius = 12f;
        private const string LocationPrefix = "SN_Namekeeper_Meadows_";

        internal static bool IsLocationName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            int cloneSuffix = name.IndexOf("(Clone)", StringComparison.Ordinal);
            if (cloneSuffix >= 0) name = name.Substring(0, cloneSuffix);
            if (!name.StartsWith(LocationPrefix, StringComparison.Ordinal)) return false;
            string suffix = name.Substring(LocationPrefix.Length);
            return suffix == "01" || suffix == "02" || suffix == "03";
        }

        internal static bool Contains(Vector3 position)
        {
            if (ContainsActiveCamp(position)) return true;
            Location location = Location.GetLocation(position, false);
            return location != null && IsLocationName(location.gameObject.name);
        }

        internal static bool ContainsActiveCamp(Vector3 position)
        {
            foreach (var camp in WorldCamp.Active)
                if (camp != null && CampFootprint.Contains(position.x, position.z,
                    camp.transform.position.x, camp.transform.position.z, Radius)) return true;
            return false;
        }

        internal static void InstallLocationFeatures(Location location)
        {
            if (location == null || !IsLocationName(location.gameObject.name)) return;

            if (location.transform.Find("NamekeeperClearGrass") == null)
            {
                var clearGrass = new GameObject("NamekeeperClearGrass");
                clearGrass.SetActive(false);
                clearGrass.transform.SetParent(location.transform, false);
                var terrainModifier = clearGrass.AddComponent<TerrainModifier>();
                terrainModifier.m_paintCleared = true;
                terrainModifier.m_paintType = TerrainModifier.PaintType.ClearVegetation;
                terrainModifier.m_square = false;
                terrainModifier.m_paintRadius = Radius + .5f;
                terrainModifier.m_paintStrength = 100f;
                terrainModifier.m_paintHeightCheck = false;
                WorldCamp.ActivateTerrain(clearGrass);
            }

            if (location.transform.Find("NamekeeperNoMonstersArea") == null)
            {
                var noMonsters = new GameObject("NamekeeperNoMonstersArea");
                noMonsters.SetActive(false);
                noMonsters.transform.SetParent(location.transform, false);
                var sphere = noMonsters.AddComponent<SphereCollider>();
                sphere.radius = Radius;
                sphere.isTrigger = true;
                var effectArea = noMonsters.AddComponent<EffectArea>();
                effectArea.m_type = EffectArea.Type.NoMonsters;
                effectArea.m_playerOnly = false;
                noMonsters.SetActive(true);
            }
        }
    }

    // ClearVegetation paints the alpha channel; native grass also uses different
    // cleared-ground rules. Reject every procedural clutter candidate inside a live
    // camp before its ground raycast, independently of paint masks/quality settings.
    [HarmonyPatch(typeof(ClutterSystem), "GetGroundInfo")]
    internal static class NamekeeperCampClutterGuard
    {
        private static bool Prefix(Vector3 __0, ref bool __result)
        {
            if (!ShopSanctuary.ContainsActiveCamp(__0)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Location), "IsInsideNoBuildLocation")]
    internal static class NamekeeperCampNoBuild
    {
        private static void Postfix(Vector3 __0, ref bool __result)
        {
            if (ShopSanctuary.Contains(__0)) __result = true;
        }
    }

    // The generated location name is the native identity used by ZoneSystem. Valheim's
    // minimap only resolves icons registered in its built-in list, so reuse Haldor's icon.
    [HarmonyPatch(typeof(Minimap), "GetLocationIcon")]
    internal static class NamekeeperLocationMapIconPatch
    {
        private static readonly MethodInfo GetLocationIconMethod =
            AccessTools.Method(typeof(Minimap), "GetLocationIcon", new[] { typeof(string) });

        private static bool Prefix(Minimap __instance, string name, ref Sprite __result)
        {
            if (!ShopSanctuary.IsLocationName(name)) return true;
            Sprite haldorIcon = GetLocationIconMethod?.Invoke(__instance, new object[] { "Vendor_BlackForest" }) as Sprite;
            if (haldorIcon == null) return true;
            __result = haldorIcon;
            return false;
        }
    }

    // Protect both the client request and the authoritative RPC/application paths. This
    // keeps the shop, roof, boar, and any other world objects in the camp from being damaged.
    [HarmonyPatch]
    internal static class NamekeeperSanctuaryDamageGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in new[] { typeof(WearNTear), typeof(Character) })
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                if ((method.Name == "Damage" || method.Name == "RPC_Damage" || method.Name == "ApplyDamage") &&
                    method.ReturnType == typeof(void))
                    yield return method;
        }

        private static bool Prefix(Component __instance)
        {
            if (__instance == null) return true;
            if (__instance is Character character && character.IsPlayer()) return true;
            return !ShopSanctuary.Contains(__instance.transform.position);
        }
    }

    [HarmonyPatch]
    internal static class NamekeeperSanctuaryBuildingDamageGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo method = AccessTools.Method(typeof(WearNTear), "ApplyDamage");
            if (method != null && method.ReturnType == typeof(bool)) yield return method;
        }

        private static bool Prefix(WearNTear __instance, ref bool __result)
        {
            if (!ShopSanctuary.Contains(__instance.transform.position)) return true;
            __result = false;
            return false;
        }
    }

    // Clear held/queued input as well as rejecting attacks so a bow or looping attack does
    // not resume unexpectedly when the player leaves the dome.
    [HarmonyPatch(typeof(Player), "PlayerAttackInput")]
    internal static class NamekeeperSanctuaryPlayerInputGuard
    {
        private static readonly FieldInfo Attack = AccessTools.Field(typeof(Character), "m_attack");
        private static readonly FieldInfo AttackHold = AccessTools.Field(typeof(Character), "m_attackHold");
        private static readonly FieldInfo SecondaryAttack = AccessTools.Field(typeof(Character), "m_secondaryAttack");
        private static readonly FieldInfo SecondaryAttackHold = AccessTools.Field(typeof(Character), "m_secondaryAttackHold");
        private static readonly FieldInfo QueuedAttack = AccessTools.Field(typeof(Player), "m_queuedAttackTimer");
        private static readonly FieldInfo QueuedSecondaryAttack = AccessTools.Field(typeof(Player), "m_queuedSecondAttackTimer");
        private static readonly FieldInfo CurrentAttack = AccessTools.Field(typeof(Humanoid), "m_currentAttack");

        private static bool Prefix(Player __instance)
        {
            if (!ShopSanctuary.Contains(__instance.transform.position)) return true;
            Set(Attack, __instance, false);
            Set(AttackHold, __instance, false);
            Set(SecondaryAttack, __instance, false);
            Set(SecondaryAttackHold, __instance, false);
            Set(QueuedAttack, __instance, 0f);
            Set(QueuedSecondaryAttack, __instance, 0f);
            (CurrentAttack?.GetValue(__instance) as Attack)?.Abort();
            return false;
        }

        private static void Set(FieldInfo field, object instance, object value)
        {
            if (field != null) field.SetValue(instance, value);
        }
    }

    [HarmonyPatch]
    internal static class NamekeeperSanctuaryHumanoidAttackGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo method = AccessTools.Method(typeof(Humanoid), "StartAttack");
            if (method != null) yield return method;
        }

        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (!(__instance is Player player) || !ShopSanctuary.Contains(player.transform.position)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class NamekeeperSanctuaryGuardianPowerGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(Player)))
                if ((method.Name == "StartGuardianPower" || method.Name == "ActivateGuardianPower") &&
                    method.ReturnType == typeof(bool))
                    yield return method;
        }

        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (!ShopSanctuary.Contains(__instance.transform.position)) return true;
            __result = false;
            return false;
        }
    }
}
