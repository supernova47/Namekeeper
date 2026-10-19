using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal static class NamekeeperInputState
    {
        internal static bool ShopOpen => NamekeeperPlugin.Instance != null &&
            NamekeeperPlugin.Instance.Ui != null && NamekeeperPlugin.Instance.Ui.InputSuppressed;
    }

    // Game input uses ZInput; IMGUI reads its own mouse events. Suppress gameplay
    // clicks while the shop is open without suppressing the IMGUI event stream.
    [HarmonyPatch]
    internal static class NamekeeperZInputBooleanGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var type = AccessTools.TypeByName("ZInput");
            if (type == null) yield break;
            foreach (var method in AccessTools.GetDeclaredMethods(type))
            {
                if (method.ReturnType != typeof(bool)) continue;
                var parameters = method.GetParameters();
                bool byName = (method.Name == "GetButton" || method.Name == "GetButtonDown" ||
                               method.Name == "GetButtonUp") &&
                               parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                bool byMouse = (method.Name == "GetMouseButton" || method.Name == "GetMouseButtonDown" ||
                                method.Name == "GetMouseButtonUp") &&
                                parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
                bool byKey = (method.Name == "GetKey" || method.Name == "GetKeyDown" ||
                              method.Name == "GetKeyUp") &&
                             parameters.Length >= 1 && parameters.Length <= 2 &&
                             parameters[0].ParameterType == typeof(KeyCode) &&
                             (parameters.Length == 1 || parameters[1].ParameterType == typeof(bool));
                if (byName || byMouse || byKey) yield return method;
            }
        }

        private static bool Prefix(ref bool __result)
        {
            if (!NamekeeperInputState.ShopOpen) return true;
            __result = false;
            return false;
        }
    }

    // The player's controller can read input separately from Player.TakeInput.
    [HarmonyPatch]
    internal static class NamekeeperPlayerControllerGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("PlayerController");
            if (type == null) yield break;
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                if (method.Name == "TakeInput" && method.ReturnType == typeof(bool))
                    yield return method;
        }

        private static bool Prefix(ref bool __result)
        {
            if (!NamekeeperInputState.ShopOpen) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class NamekeeperCameraCaptureGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var type = AccessTools.TypeByName("GameCamera");
            if (type == null) yield break;
            foreach (var method in AccessTools.GetDeclaredMethods(type))
                if (method.Name == "UpdateMouseCapture" && method.GetParameters().Length == 0)
                    yield return method;
        }

        private static bool Prefix() => !NamekeeperInputState.ShopOpen;
    }

    [HarmonyPatch]
    internal static class NamekeeperScrollWheelGuard
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var type = AccessTools.TypeByName("ZInput");
            if (type == null) yield break;
            foreach (var method in AccessTools.GetDeclaredMethods(type))
                if (method.Name == "GetMouseScrollWheel" && method.ReturnType == typeof(float)
                    && method.GetParameters().Length == 0) yield return method;
        }

        private static bool Prefix(ref float __result)
        {
            if (!NamekeeperInputState.ShopOpen) return true;
            __result = 0f;
            return false;
        }
    }
}
