using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ShopWorldLocations
    {
        internal const string WorldMode = "WorldLocations";
        internal const string BuildableMode = "HammerBuildable";
        private const string NativeTraderLocation = "Vendor_BlackForest";
        private const string LocationGroup = "SN_Namekeeper_Meadows";
        private static readonly string[] LocationNames =
        {
            "SN_Namekeeper_Meadows_01",
            "SN_Namekeeper_Meadows_02",
            "SN_Namekeeper_Meadows_03"
        };

        private static GameObject _shopPrefab;
        private static bool _registered;
        private static bool _eventHooked;
        private static bool _revealMapMarkers;
        private static bool _mapRequestPending;
        private const string MapRequestRpc = "SN_Namekeeper_MapRequest_045";
        private const string MapReplyRpc = "SN_Namekeeper_MapReply_045";
        private static ZNet _networkSession;
        private static readonly HashSet<ZRpc> RegisteredConnections = new HashSet<ZRpc>();
        private static ZRpc _pendingConnection;
        private static int _requestId;
        private static float _requestDeadline;
        private static Minimap _pinMap;
        private static readonly Minimap.PinData[] MapPins = new Minimap.PinData[3];
        private static readonly Vector3[] ServerMapPositions = new Vector3[3];
        private static readonly bool[] HasServerMapPosition = new bool[3];
        private static float _nextPinRefresh;
        private static long _worldUid;

        internal static bool IsBuildableMode => NamekeeperPlugin.OperatingMode != null &&
            string.Equals(NamekeeperPlugin.OperatingMode.Value, BuildableMode, StringComparison.OrdinalIgnoreCase);

        internal static void Register(GameObject shopPrefab)
        {
            _shopPrefab = shopPrefab;
            if (_shopPrefab == null || _registered || _eventHooked) return;
            ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
            ZoneManager.OnVanillaLocationsAvailable += RegisterLocations;
            _eventHooked = true;
        }

        private static void RegisterLocations()
        {
            if (_registered || _shopPrefab == null || ZoneManager.Instance == null) return;
            try
            {
                GameObject boarPrefab = PrefabManager.Instance.GetPrefab("Boar");
                GameObject forceFieldPrefab = PrefabManager.Instance.GetPrefab("ForceField");
                WorldCamp.LoadNativeCampSources();
                // Keep the old network identity loadable so saved 0.4.0–0.4.4 boars
                // can retire without leaving unknown-prefab ZDOs in existing worlds.
                var legacyBoar = PrefabManager.Instance.CreateClonedPrefab("NamekeeperTameBoar", boarPrefab);
                legacyBoar.AddComponent<LegacyCampBoar>();
                PrefabManager.Instance.AddPrefab(new CustomPrefab(legacyBoar, false));
                int registered = 0;
                for (int i = 0; i < LocationNames.Length; i++)
                {
                    try
                    {
                        var custom = ZoneManager.Instance.CreateClonedLocation(LocationNames[i], NativeTraderLocation);
                        if (custom == null || custom.ZoneLocation == null)
                            throw new InvalidOperationException("Could not clone the native Haldor location " + NativeTraderLocation);

                        custom.ZoneLocation.m_prefab.Load();
                        GameObject locationPrefab = custom.Prefab != null ? custom.Prefab : custom.ZoneLocation.m_prefab.Asset;
                        if (locationPrefab == null) throw new InvalidOperationException("Cloned location prefab did not load");

                        PrepareLocation(custom, locationPrefab, i, boarPrefab, forceFieldPrefab);
                        registered++;
                        NamekeeperPlugin.Log.LogInfo("Registered Meadow merchant location " + LocationNames[i]);
                    }
                    catch (Exception ex)
                    {
                        NamekeeperPlugin.Log.LogError("Could not prepare merchant location " + LocationNames[i] + ": " + ex);
                    }
                }

                _registered = registered == LocationNames.Length;
                if (!_registered)
                    NamekeeperPlugin.Log.LogError("Only " + registered + " of 3 Namekeeper Meadow locations registered; check the native Vendor_BlackForest and ForceField prefabs.");
                ApplyOperatingMode();
            }
            catch (Exception ex) { NamekeeperPlugin.Log.LogError("World merchant location registration failed: " + ex); }
            finally
            {
                ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
                _eventHooked = false;
            }
        }

        private static void PrepareLocation(Jotunn.Entities.CustomLocation custom, GameObject prefab, int index,
            GameObject boarPrefab, GameObject forceFieldPrefab)
        {
            var zone = custom.ZoneLocation;
            zone.m_enable = !IsBuildableMode;
            zone.m_biome = Heightmap.Biome.Meadows;
            zone.m_biomeArea = Heightmap.BiomeArea.Everything;
            zone.m_quantity = 1;
            zone.m_prioritized = true;
            zone.m_unique = true;
            zone.m_group = LocationGroup;
            zone.m_minDistanceFromSimilar = 1500f;
            zone.m_minDistanceFromCenter = 800f;
            zone.m_maxDistanceFromCenter = 3500f;
            zone.m_minDistance = 800f;
            zone.m_maxDistance = 3500f;
            zone.m_exteriorRadius = ShopSanctuary.Radius;
            zone.m_minAltitude = 0f;
            zone.m_maxAltitude = 1000f;
            zone.m_inForest = false;
            zone.m_forestTresholdMin = 0f;
            zone.m_forestTresholdMax = 0f;
            // A maximum of zero rejects every candidate with any vegetation. Use 1 to
            // disable this filter so Meadow locations can be placed on normal terrain.
            zone.m_minimumVegetation = 0f;
            zone.m_maximumVegetation = 1f;
            zone.m_surroundCheckVegetation = false;
            zone.m_minTerrainDelta = 0f;
            zone.m_maxTerrainDelta = 1.5f;
            zone.m_iconAlways = false;
            zone.m_iconPlaced = true;
            zone.m_clearArea = true;

            var location = custom.Location != null ? custom.Location : prefab.GetComponent<Location>();
            if (location == null) throw new InvalidOperationException("The cloned trader location has no Location component");
            location.m_exteriorRadius = ShopSanctuary.Radius;
            location.m_noBuild = true;
            location.m_noBuildRadiusOverride = 0f;
            location.m_clearArea = true;
            // ShopDiscovery shows one per-character/world/camp notice on approach.
            // A generic location label would show another banner at the dome boundary.
            location.m_discoverLabel = "";

            // Keep Haldor's native force field from the cloned trader camp, while replacing
            // the vendor and its camp props with Namekeeper's existing vanilla-piece shop.
            Transform forceField = FindForceField(prefab.transform);
            if (forceField != null) forceField.SetParent(prefab.transform, true);
            DestroyLocationChildren(prefab.transform, forceField);
            if (forceField == null)
                forceField = AddNativeForceField(prefab.transform, forceFieldPrefab);
            if (forceField == null)
                NamekeeperPlugin.Log.LogWarning("The cloned trader camp did not expose its ForceField prefab; this location will keep the native no-build and spawn-blocking settings without the dome visual.");
            var shop = Object.Instantiate(_shopPrefab, prefab.transform, false);
            shop.name = "NamekeeperWorldShop";
            shop.transform.localPosition = Vector3.zero;
            shop.transform.localRotation = Quaternion.identity;
            shop.transform.localScale = Vector3.one;
            var state = shop.GetComponent<ShopState>();
            if (state == null) throw new InvalidOperationException("World shop prefab has no ShopState");
            state.IsWorldLocation = true;
            shop.AddComponent<WorldCamp>();
            var shopView = shop.GetComponent<ZNetView>();
            if (shopView == null) throw new InvalidOperationException("World shop prefab has no ZNetView");
            shopView.m_persistent = true;
            if (forceField != null)
            {
                forceField.SetParent(shop.transform, false);
                forceField.localPosition = Vector3.zero;
                var mesh = forceField.GetComponent<MeshFilter>();
                if (mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.bounds.extents.x > .001f)
                    forceField.localScale = Vector3.one * (ShopSanctuary.Radius / mesh.sharedMesh.bounds.extents.x);
            }

        }

        private static Transform FindForceField(Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name.IndexOf("ForceField", StringComparison.OrdinalIgnoreCase) >= 0)
                    return transform;
            return null;
        }

        private static void DestroyLocationChildren(Transform root, Transform keep)
        {
            var children = new List<Transform>();
            foreach (Transform child in root) children.Add(child);
            foreach (var child in children)
                if (child != keep) Object.DestroyImmediate(child.gameObject);
        }

        private static Transform AddNativeForceField(Transform root, GameObject forceFieldPrefab)
        {
            if (forceFieldPrefab == null) return null;
            var instance = Object.Instantiate(forceFieldPrefab, root, false);
            instance.name = "NamekeeperForceField";
            instance.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 18f;
            return instance.transform;
        }

        internal static void ApplyOperatingMode()
        {
            if (ZoneManager.Instance == null) return;
            bool enabled = !IsBuildableMode;
            foreach (var name in LocationNames)
            {
                var location = ZoneManager.Instance.GetCustomLocation(name);
                if (location?.ZoneLocation != null) location.ZoneLocation.m_enable = enabled;
            }
        }

        internal static int TickMapPins(bool force = false)
        {
            EnsureMapRevealRpc();
            if (!force && Time.unscaledTime < _nextPinRefresh) return CountMapPins();
            _nextPinRefresh = Time.unscaledTime + 3f;

            // GetWorldUID dereferences the world during the loading screen in Valheim.
            long currentWorldUid = ZNet.instance != null && ZNet.instance.GetWorld() != null
                ? ZNet.instance.GetWorld().m_uid : 0L;
            if (currentWorldUid != 0L && _worldUid != 0L && currentWorldUid != _worldUid)
            {
                ClearMapPins(_pinMap);
                Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
                _mapRequestPending = false;
            }
            if (currentWorldUid != 0L) _worldUid = currentWorldUid;

            var map = Minimap.instance;
            if (map == null)
            {
                if (_pinMap != null) ClearMapPins(_pinMap);
                _pinMap = null;
                return 0;
            }
            if (_pinMap != null && _pinMap != map) ClearMapPins(_pinMap);
            _pinMap = map;
            if (!_revealMapMarkers)
            {
                ClearMapPins(map);
                return 0;
            }

            for (int i = 0; i < LocationNames.Length; i++)
            {
                if (MapPins[i] != null) continue;
                if (HasServerMapPosition[i]) AddMapPin(map, i, ServerMapPositions[i]);
            }
            return CountMapPins();
        }

        internal static int SetMapMarkersVisible(bool visible)
        {
            EnsureMapRevealRpc();
            _revealMapMarkers = visible;
            _mapRequestPending = false;
            _pendingConnection = null;
            if (_pinMap != null) ClearMapPins(_pinMap);
            Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
            if (!visible)
            {
                if (_pinMap != null) ClearMapPins(_pinMap);
                Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
                return 0;
            }

            _nextPinRefresh = 0f;
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                if (!ZNet.instance.LocalPlayerIsAdminOrHost()) return 0;
                ApplyServerMapPositions(FindServerMapPositions());
                return TickMapPins(true);
            }

            var server = ZNet.instance?.GetServerPeer();
            if (server != null && server.IsReady() && server.m_rpc != null)
            {
                _mapRequestPending = true;
                _pendingConnection = server.m_rpc;
                _requestDeadline = Time.unscaledTime + 15f;
                var request = new ZPackage();
                request.Write(++_requestId);
                server.m_rpc.Invoke(MapRequestRpc, request);
            }
            return TickMapPins(true);
        }

        internal static bool MapMarkersVisible => _revealMapMarkers;
        internal static bool MapRequestPending => _mapRequestPending;
        internal static bool MapRevealRpcReady => ZNet.instance != null &&
            (ZNet.instance.IsServer() || ZNet.instance.GetServerPeer()?.IsReady() == true);

        private static void EnsureMapRevealRpc()
        {
            var net = ZNet.instance;
            if (!ReferenceEquals(net, _networkSession))
            {
                RegisteredConnections.Clear();
                _networkSession = net;
                _mapRequestPending = false;
                _pendingConnection = null;
                _revealMapMarkers = false;
                ClearMapPins(_pinMap);
                Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
                _worldUid = 0L;
            }
            if (net == null) return;
            RegisteredConnections.RemoveWhere(rpc => !net.GetPeers().Exists(p => ReferenceEquals(p.m_rpc, rpc)));
            foreach (var peer in net.GetPeers()) RegisterConnection(peer);
            if (_mapRequestPending && Time.unscaledTime >= _requestDeadline)
            {
                _mapRequestPending = false;
                _pendingConnection = null;
                PrintMapRevealResult(L.T("The server did not reply. Check that both sides have the same Namekeeper version.",
                    "Сервер не ответил. Проверь одинаковую версию Namekeeper на клиенте и сервере."));
            }
        }

        internal static void RegisterConnection(ZNetPeer peer)
        {
            if (peer?.m_rpc == null || !RegisteredConnections.Add(peer.m_rpc)) return;
            peer.m_rpc.Register<ZPackage>(MapRequestRpc, ServerReceiveMapReveal);
            peer.m_rpc.Register<ZPackage>(MapReplyRpc, ClientReceiveMapReveal);
        }

        private static void ServerReceiveMapReveal(ZRpc rpc, ZPackage request)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            var response = new ZPackage();
            var net = ZNet.instance;
            var peer = net.GetPeers().Find(p => ReferenceEquals(p.m_rpc, rpc));
            int requestId = request.ReadInt();
            bool authorized = IsPeerAdmin(net, peer);
            response.Write(requestId);
            response.Write(authorized ? 1 : 0);

            var positions = authorized ? FindServerMapPositions() : new List<KeyValuePair<int, Vector3>>();
            response.Write(positions.Count);
            foreach (var position in positions)
            {
                response.Write(position.Key);
                response.Write(position.Value);
            }

            NamekeeperPlugin.Log.LogInfo("Shop map reveal request from " +
                (peer != null ? peer.m_playerName : "unknown peer") + ": authorized=" + authorized + ", locations=" + positions.Count);
            if (peer != null) rpc.Invoke(MapReplyRpc, response);
        }

        private static bool IsPeerAdmin(ZNet net, ZNetPeer peer)
        {
            // m_playerID identifies a character, not a Steam/PlayFab account. Bind access
            // to the authenticated transport exactly as the native admin RPCs do.
            if (net == null || peer?.m_socket == null) return false;
            return MapAdminAccess.Authorize(net.IsServer(), peer.IsReady(), peer.m_socket.IsConnected(),
                peer.m_socket.GetHostName(), net.IsAdmin);
        }

        private static void ClientReceiveMapReveal(ZRpc rpc, ZPackage response)
        {
            if (!_mapRequestPending || ZNet.instance == null || ZNet.instance.IsServer() ||
                !ReferenceEquals(rpc, _pendingConnection) ||
                !ReferenceEquals(rpc, ZNet.instance.GetServerPeer()?.m_rpc)) return;
            if (response.ReadInt() != _requestId) return;
            _mapRequestPending = false;
            _pendingConnection = null;
            int authorized = response.ReadInt();
            int count = response.ReadInt();
            if (count < 0 || count > LocationNames.Length)
            {
                NamekeeperPlugin.Log.LogWarning("Server returned an invalid shop-location count: " + count);
                return;
            }

            if (authorized == 0)
            {
                _revealMapMarkers = false;
                PrintMapRevealResult(L.T("The server denied this request. Check that you are listed as an administrator.",
                    "Сервер отклонил запрос. Проверь, что ты добавлен в список администраторов."));
                return;
            }

            if (!_revealMapMarkers) return;
            Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
            for (int i = 0; i < count; i++)
            {
                int index = response.ReadInt();
                Vector3 position = response.ReadVector3();
                if (index < 0 || index >= LocationNames.Length) continue;
                ServerMapPositions[index] = position;
                HasServerMapPosition[index] = true;
            }

            int shown = TickMapPins(true);
            if (count == 0)
            {
                PrintMapRevealResult(L.T("The server has no generated Namekeeper shop locations in this world yet.",
                    "Сервер пока не нашёл сгенерированные лавки имён в этом мире."));
            }
            else
            {
                PrintMapRevealResult(L.T("Showing " + shown + " of " + LocationNames.Length + " generated Namekeeper shop markers on your map.",
                    "На твоей карте показано меток лавок: " + shown + " из " + LocationNames.Length + "."));
            }
            NamekeeperPlugin.Log.LogInfo("Received " + count + " generated shop map positions from the server; displayed " + shown + ".");
        }

        private static List<KeyValuePair<int, Vector3>> FindServerMapPositions()
        {
            var result = new List<KeyValuePair<int, Vector3>>();
            var zoneSystem = ZoneSystem.instance;
            if (zoneSystem == null || !zoneSystem.LocationsGenerated || IsBuildableMode) return result;
            for (int i = 0; i < LocationNames.Length; i++)
            {
                var matches = new List<ZoneSystem.LocationInstance>();
                if (zoneSystem.FindLocations(LocationNames[i], ref matches) && matches != null && matches.Count > 0)
                    result.Add(new KeyValuePair<int, Vector3>(i, matches[0].m_position));
            }
            return result;
        }

        private static void ApplyServerMapPositions(List<KeyValuePair<int, Vector3>> positions)
        {
            Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
            foreach (var position in positions)
            {
                if (position.Key < 0 || position.Key >= LocationNames.Length) continue;
                ServerMapPositions[position.Key] = position.Value;
                HasServerMapPosition[position.Key] = true;
            }
        }

        private static void AddMapPin(Minimap map, int index, Vector3 position)
        {
            string label = L.T("Namekeeper's Shop", "Лавка имён") + " " + (index + 1);
            MapPins[index] = map.AddPin(position, Minimap.PinType.Hildir1, label, false, false, 0L,
                default(Splatform.PlatformUserID));
        }

        private static int CountMapPins()
        {
            int count = 0;
            foreach (var pin in MapPins) if (pin != null) count++;
            return count;
        }

        private static void PrintMapRevealResult(string message)
        {
            if (global::Console.instance != null) global::Console.instance.Print(message);
            else NamekeeperPlugin.Log.LogInfo(message);
        }

        private static void ClearMapPins(Minimap map)
        {
            if (map != null)
                foreach (var pin in MapPins)
                    if (pin != null) map.RemovePin(pin);
            Array.Clear(MapPins, 0, MapPins.Length);
        }

        internal static void Dispose()
        {
            if (_eventHooked) ZoneManager.OnVanillaLocationsAvailable -= RegisterLocations;
            _eventHooked = false;
            ClearMapPins(_pinMap);
            _pinMap = null;
            _revealMapMarkers = false;
            _mapRequestPending = false;
            _pendingConnection = null;
            _networkSession = null;
            RegisteredConnections.Clear();
            _worldUid = 0L;
            Array.Clear(HasServerMapPosition, 0, HasServerMapPosition.Length);
            _nextPinRefresh = 0f;
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    internal static class NamekeeperMapConnectionPatch
    {
        private static void Postfix(ZNetPeer __0) => ShopWorldLocations.RegisterConnection(__0);
    }
}
