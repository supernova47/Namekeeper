using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    // The shop ZDO is the durable camp anchor. LocationProxy roots are temporary and
    // are absent when a saved network shop is instantiated independently by a client.
    internal sealed class WorldCamp : MonoBehaviour
    {
        internal const string LevelKey = "sn_namekeeper_level_045";
        internal static int NewCampDepth;
        internal static readonly HashSet<WorldCamp> Active = new HashSet<WorldCamp>();
        private static readonly List<SoftReferenceableAssets.SoftReference<GameObject>> NativeReferences =
            new List<SoftReferenceableAssets.SoftReference<GameObject>>();
        private static readonly FieldInfo TriggerOnPlaced = AccessTools.Field(typeof(TerrainModifier), "m_triggerOnPlaced");
        private ZNetView _view;
        private bool _ready;
        private float _nextAttempt;
        private float _nextAnimalAttempt;
        private readonly CampBoar[] _boars = new CampBoar[2];
        private readonly string[] _animalErrors = new string[2];

        internal static void LoadNativeCampSources()
        {
            foreach (string name in new[] { "Vendor_BlackForest", "Hildir_camp" })
            {
                var native = ZoneManager.Instance.GetZoneLocation(name);
                if (native == null) continue;
                native.m_prefab.Load();
                NativeReferences.Add(native.m_prefab);
                var root = native.m_prefab.Asset;
                if (root == null) continue;
                if (name == "Hildir_camp")
                {
                    try { CampMeal.Capture(root); }
                    catch (Exception ex) { NamekeeperPlugin.Log.LogWarning("Camp native meal unavailable: " + ex.Message); }
                }
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    // These are original vanilla authored meshes, not resource-bearing pieces.
                    if (child.name == "TraderRune" || child.name == "hildir_tent2" ||
                        child.name == "hildir_barrel" || child.name == "fi_vil_container_bucket01")
                        NativeWorkshopAssets.Remember(child.name, child.gameObject);
                }
            }
        }

        internal static void CaptureGeneration(ZNetView view)
        {
            if (NewCampDepth == 0 || view == null || !view.IsValid() || !view.IsOwner()) return;
            var state = view.GetComponent<ShopState>();
            if (state != null && state.IsWorldLocation) view.GetZDO().Set(LevelKey, 1);
        }

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            CaptureGeneration(_view);
        }

        private void Update()
        {
            if (_ready) { EnsureBoars(); return; }
            if (Time.time < _nextAttempt) return;
            _nextAttempt = Time.time + 3f;
            if (_view == null || !_view.IsValid() || ZNet.instance == null ||
                ZNet.instance.GetWorld() == null || Heightmap.FindHeightmap(transform.position) == null) return;
            try
            {
                Active.Add(this);
                InstallTerrainAndArea();
                _view.Register<int>("SN_Namekeeper_Pet", ReceivePet);
                _ready = true;
                // A visual prop must not prevent interaction/animals from initializing.
                try { BuildDecor(); }
                catch (Exception ex) { NamekeeperPlugin.Log.LogError("Camp decor setup failed: " + ex); }
                EnsureBoars();
                NamekeeperPlugin.Log.LogInfo("Namekeeper camp ready at " + transform.position +
                    "; terrain=" + (_view.GetZDO().GetInt(LevelKey, 0) != 0 ? "leveled new camp" : "preserved existing camp") +
                    "; local pet displays=" + ((_boars[0] != null ? 1 : 0) + (_boars[1] != null ? 1 : 0)) + ".");
            }
            catch (Exception ex)
            {
                // Do not repeatedly clone a partial camp if an optional native asset is missing.
                _ready = true;
                NamekeeperPlugin.Log.LogError("Could not finish Namekeeper camp at " + transform.position + ": " + ex);
            }
        }

        private void EnsureBoars()
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated() || Time.time < _nextAnimalAttempt) return;
            _nextAnimalAttempt = Time.time + 5f;
            for (int i = 0; i < _boars.Length; i++)
            {
                if (_boars[i] != null) continue;
                try
                {
                    _boars[i] = CampBoar.Create(this, i, GroundPoint(i == 0 ? 5.25f : 6.75f, -3f));
                    _animalErrors[i] = null;
                    NamekeeperPlugin.Log.LogInfo("Created camp boar " + i + " at " + _boars[i].transform.position);
                }
                catch (Exception ex)
                {
                    if (_animalErrors[i] != ex.Message)
                        NamekeeperPlugin.Log.LogError("Camp boar " + i + " setup failed; will retry: " + ex);
                    _animalErrors[i] = ex.Message;
                }
            }
        }

        private void InstallTerrainAndArea()
        {
            var area = new GameObject("SN_Namekeeper_Meadows_01");
            area.SetActive(false);
            area.transform.SetParent(transform, false);
            var location = area.AddComponent<Location>();
            location.m_exteriorRadius = ShopSanctuary.Radius;
            location.m_noBuild = true;
            location.m_clearArea = true;
            location.m_discoverLabel = ""; // Discovery is handled alongside the native map pin.
            ShopSanctuary.InstallLocationFeatures(location);
            ActivateTerrain(area);

            // Only ZDOs born during NEW ZoneSystem generation receive this key. Missing
            // means an existing camp: grass clearing is allowed, height changes are not.
            if (_view.GetZDO().GetInt(LevelKey, 0) != 0)
            {
                var terrain = new GameObject("NamekeeperNewCampTerrain");
                terrain.SetActive(false);
                terrain.transform.SetParent(transform, false);
                var modifier = terrain.AddComponent<TerrainModifier>();
                modifier.m_level = true;
                modifier.m_levelRadius = 9.5f;
                modifier.m_levelOffset = 0f;
                modifier.m_square = false;
                modifier.m_smooth = true;
                modifier.m_smoothRadius = 11.5f;
                modifier.m_smoothPower = 3f;
                modifier.m_paintCleared = false;
                ActivateTerrain(terrain);
            }
            // Native modifiers normally rebuild heightmaps two LateUpdates later.
            // Finish that rebuild before sampling the ground for props and animals.
            var maps = new List<Heightmap>();
            Heightmap.FindHeightmap(transform.position, ShopSanctuary.Radius + 1f, maps);
            foreach (var map in maps) map.Poke(0, false);
            ClutterSystem.instance?.ResetGrass(transform.position, ShopSanctuary.Radius + 2f);
        }

        internal static void ActivateTerrain(GameObject terrain)
        {
            // OnPlaced removes other modifiers. Loading a camp must never invoke that
            // placement-time side effect against existing terrain or player landscaping.
            bool previous = (bool)TriggerOnPlaced.GetValue(null);
            try { TriggerOnPlaced.SetValue(null, false); terrain.SetActive(true); }
            finally { TriggerOnPlaced.SetValue(null, previous); }
        }

        internal Vector3 GroundPoint(float x, float z)
        {
            Vector3 point = transform.TransformPoint(new Vector3(x, 0f, z));
            var map = Heightmap.FindHeightmap(point);
            if (map != null && Heightmap.GetHeight(point, out float height)) point.y = height;
            return point;
        }

        private Transform Prop(string prefab, float x, float z, float yaw = 0f, bool solid = true)
        {
            Transform part = null;
            try
            {
                part = NativeWorkshopAssets.Add(transform, prefab, Vector3.zero, Vector3.one, yaw, solid);
                part.position = GroundPoint(x, z);
                // Use the actual geometry bottom, not guessed prefab pivots. Preserve the
                // original horizontal origin so tent poles and parented supplies stay aligned.
                float bottom = float.PositiveInfinity;
                foreach (var filter in part.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || filter.GetComponent<Renderer>() == null) continue;
                    var bounds = filter.sharedMesh.bounds;
                    for (int ix = -1; ix <= 1; ix += 2)
                    for (int iy = -1; iy <= 1; iy += 2)
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        var p = filter.transform.TransformPoint(bounds.center +
                            Vector3.Scale(bounds.extents, new Vector3(ix, iy, iz)));
                        bottom = Mathf.Min(bottom, p.y);
                    }
                }
                if (!float.IsInfinity(bottom)) part.position += Vector3.up * (part.position.y - bottom);
                return part;
            }
            catch (Exception ex)
            {
                if (part != null) Object.Destroy(part.gameObject);
                NamekeeperPlugin.Log.LogWarning("Camp decoration unavailable (" + prefab + "): " + ex.Message);
                return null;
            }
        }

        private void BuildDecor()
        {
            // Approved top-down layout, metres relative to the existing shop. -Z is front.
            Prop("hildir_tent2", -5.7f, 1.3f);
            Prop("hildir_barrel", -6.4f, 2.4f);
            Prop("hildir_barrel", -4.7f, 2.4f, 20f);
            Prop("dvergrprops_crate", -6.7f, .4f, 10f);
            Prop("dvergrprops_crate", -5.5f, .4f, -15f);
            Prop("wood_stack", 5.5f, 2.1f, 90f);
            Prop("dvergrprops_crate", 7.3f, .6f, 25f);
            var bench = Prop("piece_bench01", -6.6f, -5.2f, 90f);
            if (bench != null)
            {
                try { CampSeating.Install(bench); }
                catch (Exception ex) { NamekeeperPlugin.Log.LogWarning("Camp seating unavailable: " + ex.Message); }
            }
            var table = Prop("piece_table", -5f, -5.2f, 90f);
            if (table != null && !ZNet.instance.IsDedicated()) SetTable(table);
            var fire = Prop("fire_pit_haldor", -3f, -5.2f);
            if (fire != null && !ZNet.instance.IsDedicated()) AddFireVisual(fire);
            foreach (float angle in new[] { 30f, 60f, 90f, 120f, 240f, 270f, 300f, 330f })
            {
                float radians = angle * Mathf.Deg2Rad;
                Prop("TraderRune", 11f * Mathf.Sin(radians), 11f * Mathf.Cos(radians), angle);
            }
            try
            {
                CampMeal.Create(transform, GroundPoint(6f, -4.4f));
            }
            catch (Exception ex) { NamekeeperPlugin.Log.LogWarning("Camp shared bowl unavailable: " + ex.Message); }
        }

        private static void AddFireVisual(Transform fire)
        {
            var source = NativeWorkshopAssets.Source("fire_pit_haldor").transform.Find("_enabled_high");
            if (source == null) return;
            var holder = new GameObject("CampFireVisual");
            holder.SetActive(false);
            holder.transform.SetParent(fire, false);
            var copy = Object.Instantiate(source.gameObject, holder.transform, false);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            copy.SetActive(true);
            holder.SetActive(true);
            AddFireAudio(source, holder.transform);
        }

        private void SetTable(Transform table)
        {
            Bounds top = MeshBoundsIn(table, table);
            // Full-size native items, spaced along the table's long axis. The meshes'
            // actual lower bounds rest on the tabletop; there are no ItemDrop scripts.
            string[] items = { "Bread", "CookedMeat", "Tankard" };
            float[] places = { -.8f, 0f, .8f };
            for (int i = 0; i < items.Length; i++)
            {
                Transform item = null;
                try
                {
                    item = NativeWorkshopAssets.AddItemVisual(table, items[i]);
                    Bounds bounds = MeshBoundsIn(item, table);
                    item.localPosition += new Vector3(places[i], top.max.y + .002f + bounds.extents.y, 0) - bounds.center;
                }
                catch (Exception ex)
                {
                    if (item != null) Object.Destroy(item.gameObject);
                    NamekeeperPlugin.Log.LogWarning("Camp table item unavailable (" + items[i] + "): " + ex.Message);
                }
            }
        }

        internal static Bounds MeshBoundsIn(Transform root, Transform relativeTo)
        {
            Bounds result = default;
            bool found = false;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;
                Bounds mesh = filter.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = relativeTo.InverseTransformPoint(filter.transform.TransformPoint(mesh.center +
                        Vector3.Scale(mesh.extents, new Vector3(x, y, z))));
                    if (!found) { result = new Bounds(corner, Vector3.zero); found = true; }
                    else result.Encapsulate(corner);
                }
            }
            if (!found) throw new InvalidOperationException("No decoration mesh bounds: " + root.name);
            return result;
        }

        private static void AddFireAudio(Transform source, Transform target)
        {
            foreach (var sfx in source.GetComponentsInChildren<ZSFX>(true))
            {
                if (sfx.m_audioClips == null || sfx.m_audioClips.Length == 0 || sfx.m_audioClips[0] == null) continue;
                var audio = target.gameObject.AddComponent<AudioSource>();
                var native = sfx.GetComponent<AudioSource>();
                if (native != null) audio.outputAudioMixerGroup = native.outputAudioMixerGroup;
                audio.clip = sfx.m_audioClips[0];
                audio.spatialBlend = 1f;
                audio.volume = .25f;
                audio.minDistance = 1.5f;
                audio.maxDistance = 15f;
                audio.loop = true;
                audio.Play();
                return;
            }
        }

        internal void Pet(int index)
        {
            if (index < 0 || index >= _boars.Length || Player.m_localPlayer == null) return;
            _view.InvokeRPC(ZNetView.Everybody, "SN_Namekeeper_Pet", index);
        }

        private void ReceivePet(long sender, int index)
        {
            if (index >= 0 && index < _boars.Length) _boars[index]?.React();
        }

        private void OnDestroy()
        {
            Active.Remove(this);
            if (_view != null) _view.Unregister("SN_Namekeeper_Pet");
        }
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class NamekeeperNewCampMarker
    {
        private static void Postfix(ZNetView __instance) => WorldCamp.CaptureGeneration(__instance);
    }

    [HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
    internal static class NamekeeperCampGenerationContext
    {
        private static void Prefix(ZoneSystem.ZoneLocation location, ZoneSystem.SpawnMode mode, out int __state)
        {
            __state = WorldCamp.NewCampDepth;
            // Client proxy loading must never mark an existing shop as newly generated.
            if (mode == ZoneSystem.SpawnMode.Client) WorldCamp.NewCampDepth = 0;
            else if (ShopSanctuary.IsLocationName(location.m_prefabName)) WorldCamp.NewCampDepth++;
        }
        private static Exception Finalizer(Exception __exception, int __state)
        {
            WorldCamp.NewCampDepth = __state;
            return __exception;
        }
    }

    internal sealed class LegacyCampBoar : MonoBehaviour
    {
        private void Awake()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (GetComponent<BaseAI>() != null) GetComponent<BaseAI>().enabled = false;
            if (GetComponent<Tameable>() != null) GetComponent<Tameable>().enabled = false;
            if (GetComponent<Character>() != null) GetComponent<Character>().enabled = false;
            if (GetComponent<Rigidbody>() != null) GetComponent<Rigidbody>().isKinematic = true;
        }

        private void Update()
        {
            var view = GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            // Only the legacy Namekeeper-specific prefab has this component. Never
            // remove ordinary player animals or generate loot during migration.
            ZNetScene.instance?.Destroy(gameObject);
        }
    }
}
