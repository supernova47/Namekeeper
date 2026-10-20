using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    [HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
    internal static class ShopDiscovery
    {
        private const float NoticeRadius = 150f;
        private static float _nextCheck;
        private static Minimap _map;
        private static readonly Dictionary<Vector3, string> Locations = new Dictionary<Vector3, string>();

        private static void Postfix(Minimap __instance, Dictionary<Vector3, Minimap.PinData> ___m_locationPins)
        {
            var player = Player.m_localPlayer;
            var net = ZNet.instance;
            if (player == null || net == null || net.IsDedicated() || net.GetWorld() == null ||
                ZoneSystem.instance == null || MessageHud.instance == null) return;
            if (_map != __instance) { _map = __instance; _nextCheck = 0; }
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 1f;
            Locations.Clear();
            ZoneSystem.instance.GetLocationIcons(Locations);
            foreach (var location in Locations)
            {
                if (!ShopSanctuary.IsLocationName(location.Value) ||
                    !___m_locationPins.TryGetValue(location.Key, out var pin) || pin.m_icon == null ||
                    !CampFootprint.Contains(player.transform.position.x, player.transform.position.z,
                        location.Key.x, location.Key.z, NoticeRadius)) continue;
                if (!CampDiscoveryMemory.Remember(player.m_customData, net.GetWorld().m_uid,
                    Mathf.RoundToInt(location.Key.x * 10), Mathf.RoundToInt(location.Key.z * 10))) continue;
                MessageHud.instance.ShowBiomeFoundMsg(L.T("Namekeeper's Shop discovered", "Вы нашли лавку имён"), true);
                player.Message(MessageHud.MessageType.TopLeft,
                    L.T("Namekeeper's Shop marked on your map", "Лавка имён отмечена на карте"), 0, pin.m_icon);
            }
        }
    }
}
