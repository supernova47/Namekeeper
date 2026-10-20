using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    // Copy only engine geometry components. Native Awake/OnEnable, network views,
    // fuel, containers, item drops and piece scripts never enter the new hierarchy.
    internal static class NativeWorkshopAssets
    {
        private static readonly Dictionary<string, GameObject> Sources = new Dictionary<string, GameObject>();

        internal static void Remember(string name, GameObject source)
        {
            if (source != null) Sources[name] = source;
        }

        internal static GameObject Source(string name)
        {
            if (Sources.TryGetValue(name, out var cached) && cached != null) return cached;
            var source = PrefabManager.Instance.GetPrefab(name);
            if (source == null) throw new InvalidOperationException("Native workshop prefab unavailable: " + name);
            Sources[name] = source;
            return source;
        }

        internal static Transform Add(Transform parent, string prefab, Vector3 position, Vector3 scale,
            float yaw = 0, bool solid = true, bool roof = false, bool includeDoorLeaf = false)
        {
            var source = Source(prefab);
            var wear = source.GetComponent<WearNTear>();
            var healthy = wear != null && wear.m_new != null ? wear.m_new.transform : source.transform;
            var doorLeaf = includeDoorLeaf ? source.transform.Find("door") : null;
            var selected = new HashSet<Renderer>();
            var allLods = new HashSet<Renderer>();
            var firstLods = new HashSet<Renderer>();
            // Floors keep their LODGroup on the prefab root, outside m_new.
            foreach (var group in source.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                for (int i = 0; i < lods.Length; i++)
                    foreach (var renderer in lods[i].renderers)
                    {
                        if (renderer == null) continue;
                        allLods.Add(renderer);
                        if (i == 0) firstLods.Add(renderer);
                    }
            }
            SelectMeshes(healthy, allLods, firstLods, selected);
            // The vanilla wooden door animates a separate child outside WearNTear.m_new.
            // Include that leaf explicitly while still copying no native scripts.
            if (doorLeaf != null) SelectMeshes(doorLeaf, allLods, firstLods, selected);
            if (includeDoorLeaf && doorLeaf == null)
                throw new InvalidOperationException("Native wooden door leaf unavailable");
            if (selected.Count == 0) throw new InvalidOperationException("No healthy native meshes: " + prefab);
            int layer = LayerMask.NameToLayer("piece");
            if (layer < 0) layer = 0;
            var copy = CopyNode(source.transform, parent, source.transform, wear, selected, solid, roof, layer);
            copy.name = "Native_" + prefab;
            copy.localPosition = position;
            copy.localRotation = Quaternion.Euler(0, yaw, 0);
            copy.localScale = scale;
            return copy;
        }

        internal static Transform AddItemVisual(Transform parent, string prefab)
        {
            var source = Source(prefab);
            var attach = source.transform.Find("attach");
            if (attach == null) throw new InvalidOperationException("Native item attach missing: " + prefab);
            var selected = new HashSet<Renderer>();
            foreach (var renderer in attach.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) selected.Add(renderer);
            }
            if (selected.Count == 0) throw new InvalidOperationException("Native item attach has no meshes: " + prefab);
            var root = new GameObject("NativeItem_" + prefab);
            root.transform.SetParent(parent, false);
            try
            {
                // The full prefab also contains an inactive equipoffset model 51 m below
                // its origin and unrelated LOD references. Use only the actual item visual.
                // sourceRoot stays the prefab root so attach's authored scale is retained.
                int layer = LayerMask.NameToLayer("piece");
                CopyNode(attach, root.transform, source.transform, null, selected, false, false, layer < 0 ? 0 : layer);
                if (root.GetComponentsInChildren<MeshRenderer>(true).Length == 0)
                    throw new InvalidOperationException("Item visual copy produced no renderers: " + prefab);
                return root.transform;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        private static void SelectMeshes(Transform root, HashSet<Renderer> allLods,
            HashSet<Renderer> firstLods, HashSet<Renderer> selected)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (allLods.Contains(renderer) ? !firstLods.Contains(renderer) : !renderer.enabled) continue;
                bool snow = false;
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && material.shader != null && material.shader.name.IndexOf("Snow", StringComparison.OrdinalIgnoreCase) >= 0) snow = true;
                if (!snow) selected.Add(renderer);
            }
        }

        private static Transform CopyNode(Transform source, Transform parent, Transform sourceRoot, WearNTear wear,
            HashSet<Renderer> selected, bool solid, bool roof, int layer)
        {
            var node = new GameObject(source.name) { layer = layer, tag = roof ? "Untagged" : "leaky" };
            node.transform.SetParent(parent, false);
            if (source != sourceRoot)
            {
                node.transform.localPosition = source.localPosition;
                node.transform.localRotation = source.localRotation;
                node.transform.localScale = source.localScale;
            }
            var renderer = source.GetComponent<MeshRenderer>();
            var filter = source.GetComponent<MeshFilter>();
            if (renderer != null && selected.Contains(renderer) && filter != null && filter.sharedMesh != null)
            {
                node.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var target = node.AddComponent<MeshRenderer>();
                target.sharedMaterials = renderer.sharedMaterials;
                target.shadowCastingMode = renderer.shadowCastingMode;
                target.receiveShadows = renderer.receiveShadows;
            }
            if (solid)
                foreach (var collider in source.GetComponents<Collider>())
                    if (collider.enabled && !collider.isTrigger) CopyCollider(collider, node);
            foreach (Transform child in source)
            {
                if (child.CompareTag("snappoint")) continue;
                if (!child.gameObject.activeSelf && (wear == null || wear.m_new == null || child != wear.m_new.transform) &&
                    !HasSelectedMesh(child, selected)) continue;
                if (wear != null && ((wear.m_worn != null && child == wear.m_worn.transform) ||
                    (wear.m_broken != null && child == wear.m_broken.transform) ||
                    (wear.m_wet != null && child == wear.m_wet.transform))) continue;
                // Non-rendering children are harmless transforms; no behaviours are cloned.
                CopyNode(child, node.transform, sourceRoot, wear, selected, solid, roof, layer);
            }
            if (source != sourceRoot && node.transform.childCount == 0 && node.GetComponent<Renderer>() == null && node.GetComponent<Collider>() == null)
            {
                UnityEngine.Object.DestroyImmediate(node);
                return null;
            }
            return node.transform;
        }

        private static bool HasSelectedMesh(Transform root, HashSet<Renderer> selected)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (selected.Contains(renderer)) return true;
            return false;
        }

        private static void CopyCollider(Collider source, GameObject node)
        {
            Collider copy;
            if (source is BoxCollider box)
            { var c = node.AddComponent<BoxCollider>(); c.center = box.center; c.size = box.size; copy = c; }
            else if (source is MeshCollider mesh)
            { var c = node.AddComponent<MeshCollider>(); c.sharedMesh = mesh.sharedMesh; c.convex = mesh.convex; copy = c; }
            else if (source is SphereCollider sphere)
            { var c = node.AddComponent<SphereCollider>(); c.center = sphere.center; c.radius = sphere.radius; copy = c; }
            else if (source is CapsuleCollider capsule)
            { var c = node.AddComponent<CapsuleCollider>(); c.center = capsule.center; c.radius = capsule.radius; c.height = capsule.height; c.direction = capsule.direction; copy = c; }
            else throw new InvalidOperationException("Unsupported native collider: " + source.GetType().Name);
            copy.sharedMaterial = source.sharedMaterial;
        }

        internal static Material SignMaterial()
        {
            var source = Source("sign");
            var wear = source.GetComponent<WearNTear>();
            var root = wear != null && wear.m_new != null ? wear.m_new : source;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && material.shader != null && material.HasProperty("_MainTex") && material.mainTexture != null)
                        return material;
            throw new InvalidOperationException("Native sign wood material unavailable");
        }
    }
}
