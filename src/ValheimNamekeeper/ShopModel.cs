using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ShopModel
    {
        internal static void BuildBody(Transform root) => Build(root, false);
        internal static void BuildRoof(Transform root) => Build(root, true);

        private static void Build(Transform root, bool roof)
        {
            var holder = new GameObject(roof ? "NativeRoof" : "NativeWorkshop");
            holder.SetActive(false);
            holder.transform.SetParent(root, false);
            try
            {
                if (roof) Roof(holder.transform); else Body(holder.transform);
                holder.SetActive(true);
                NamekeeperPlugin.Log.LogInfo("Native workshop " + (roof ? "roof" : "body") + " geometry prepared");
            }
            catch (Exception ex)
            {
                Object.DestroyImmediate(holder);
                // A cosmetic failure must never remove persistent prefab identities.
                NamekeeperPlugin.Log.LogWarning("Native workshop geometry unavailable; preserving legacy model: " + ex.Message);
                if (roof) LegacyShopModel.BuildRoof(root); else LegacyShopModel.BuildBody(root);
            }
        }

        private static Transform Part(Transform root, string source, float x, float y, float z,
            float sx = 1, float sy = 1, float sz = 1, float yaw = 0, bool solid = true, bool roof = false,
            bool includeDoorLeaf = false) =>
            NativeWorkshopAssets.Add(root, source, new Vector3(x,y,z), new Vector3(sx,sy,sz), yaw, solid, roof, includeDoorLeaf);

        private static void Beam(Transform root, Vector3 position, Vector3 size, bool roof = false, bool darkwood = false, bool solid = true)
        {
            // Native beams are about 2m long; darkwood uses a slightly wider cross-section.
            float nativeY = darkwood ? .41f : .4f;
            float nativeZ = darkwood ? .49f : .4f;
            NativeWorkshopAssets.Add(root, darkwood ? "darkwood_beam" : "wood_beam", position,
                new Vector3(size.x / 2, size.y / nativeY, size.z / nativeZ), 0, solid, roof);
        }

        private static void Body(Transform root)
        {
            foreach (float x in new[] { -1.175f, 1.175f })
            foreach (float z in new[] { -1.0125f, 1.0125f })
                Part(root,"wood_floor",x,-.08f,z,1.175f,1,1.0125f);
            foreach (float x in new[] { -2.05f, 2.05f })
            foreach (float z in new[] { -1.64f, 1.64f })
            {
                // Keep the floor at its established height while bringing its four feet closer to it.
                Part(root,"wood_pole",x,-.31f,z,.90f,.54f,.90f);
                Part(root,"wood_pole",x,1.46f,z,.625f,3.15f,.625f);
                var brace = Part(root,"wood_beam",x-Mathf.Sign(x)*.30f,2.56f,z,.44f,.375f,.45f,solid:false);
                brace.localRotation = Quaternion.Euler(0,0,Mathf.Sign(x)*43);
            }
            // Leave a correctly sized opening in the rear wall for the hinged vanilla door.
            foreach (float x in new[] { -1.475f, 1.475f })
                Part(root,"woodwall",x,1,1.82f,.75f);
            foreach (float x in new[] { -1.125f, 1.125f })
            {
                Part(root,"wood_wall_half",x,2.45f,1.82f,1.125f,.9f);
            }
            var doorRoot = Part(root,"wood_door",.014f,1f,1.82f,1,1,1,includeDoorLeaf:true);
            var doorLeaf = doorRoot.Find("door");
            if (doorLeaf == null) throw new InvalidOperationException("Native door leaf was not copied");
            var door = doorLeaf.gameObject.AddComponent<WorkshopDoor>();
            door.Initialize(NativeWorkshopAssets.Source("wood_door").GetComponent<Door>());
            foreach (float x in new[] { -2.22f, 2.22f })
            foreach (float z in new[] { -.91f, .91f })
                Part(root,"wood_wall_half",x,.825f,z,.91f,1.65f,1,90);
            foreach (float x in new[] { -1.025f, 1.025f })
            {
                Part(root,"wood_floor",x,1.06f,-1.46f,1.025f,1,.475f);
                Part(root,"wood_wall_half",x,.50f,-1.91f,1.025f,1);
            }
            Beam(root,new Vector3(0,1.12f,-1.92f),new Vector3(4.5f,.17f,.19f));
            foreach (float z in new[] { -1.9f, 1.9f })
                Beam(root,new Vector3(0,2.90f,z),new Vector3(4.6f,.16f,.21f));
            foreach (float x in new[] { -2.22f, 2.22f })
                Beam(root,new Vector3(x,1.98f,0),new Vector3(.22f,.19f,3.6f));
            // The board itself is the ordinary Valheim sign, with no Sign/ZNetView scripts.
            Part(root,"sign",0,2.64f,-1.98f,1.85f,.88f,1.5f,solid:false);
            Beam(root,new Vector3(0,2.91f,-1.98f),new Vector3(2.06f,.09f,.20f));
            foreach (float x in new[] { -.9f, .9f })
                Part(root,"wood_pole",x,2.90f,-1.98f,.2f,.45f,.25f,solid:false);
            var plaque = new GameObject("LocalizedWorkshopSign");
            plaque.transform.SetParent(root,false);
            plaque.transform.localPosition = new Vector3(0,2.64f,-2.059f);
            plaque.AddComponent<WorkshopSign>().Initialize();
            // Full-size vanilla bed: its 3.1m length fits along the left side of the shop.
            DisplayOnSurface(root,"bed",-1.25f,.002f,-.15f);
            foreach (float y in new[] { .72f,1.42f,2.13f })
            {
                // Move each shelf back to the wall and join it to a visible wooden wall cleat.
                Part(root,"wood_floor",1.16f,y-.049f,1.31f,.74f,.6f,.215f);
                Beam(root,new Vector3(1.16f,y-.065f,1.50f),new Vector3(1.40f,.09f,.11f),solid:false);
            }
            Stock(root);
            WallDisplays(root);
            foreach (float x in new[] { -2.05f,2.05f }) Lantern(root,x);
        }

        private static void Stock(Transform root)
        {
            // These are visual-only copies: no drops, inventory, item scripts or colliders.
            const float shelfZ = 1.25f;
            DisplayOnSurface(root,"Amber",.70f,.72f,shelfZ,0f,1.50f);
            DisplayOnSurface(root,"Coins",1.16f,.72f,shelfZ,0f,1.50f);
            DisplayOnSurface(root,"Ruby",1.66f,.72f,shelfZ,0f,1.50f);

            DisplayOnSurface(root,"AmberPearl",.70f,1.42f,shelfZ,0f,1.50f);
            DisplayOnSurface(root,"Ruby",1.18f,1.42f,shelfZ,0f,1.50f);
            DisplayOnSurface(root,"Coins",1.66f,1.42f,shelfZ,0f,1.50f);

            DisplayOnSurface(root,"Coins",.65f,2.13f,shelfZ,0f,1.50f);
            // Turn the native deer trophy toward the customer, with its backing near the wall.
            DisplayOnSurface(root,"TrophyDeer",1.22f,2.13f,1.22f,180f,1.50f);
            DisplayOnSurface(root,"Amber",1.76f,2.13f,shelfZ,0f,1.50f);

            // The full-size chest belongs on the serving counter, not squeezed onto a shelf.
            DisplayOnSurface(root,"piece_chest_wood",1.18f,1.14f,-1.56f);
        }

        private static void WallDisplays(Transform root)
        {
            try
            {
                // Item meshes are authored flat in XZ. Rotate their native orientation upright
                // and face the customer; each stays in the clear side panel beside the door.
                // Keep the back faces a few millimetres clear of the wall's inner face (Z 1.543).
                var shield = Part(root,"ShieldWood",-1.62f,1.30f,1.505f,1,1,1,0,false);
                shield.localRotation = Quaternion.Euler(-90f,0f,0f);
                var sword = Part(root,"SwordBronze",-.95f,1.30f,1.525f,1,1,1,0,false);
                sword.localRotation = Quaternion.Euler(-90f,0f,0f);
            }
            catch (Exception ex)
            {
                // One optional wall display must never discard the complete native shop.
                NamekeeperPlugin.Log.LogWarning("Optional shop wall display unavailable: " + ex.Message);
            }
        }

        private static Transform DisplayOnSurface(Transform root, string prefab, float x, float surfaceY,
            float z, float yaw = 0f, float maxBackZ = float.NaN)
        {
            Transform item = null;
            try
            {
                // Keep every decorative prefab at its authored scale. Find its real mesh bounds
                // so models rest on the shelf and stay in front of the rear wall.
                item = Part(root,prefab,x,0f,z,1f,1f,1f,yaw,false);
                float lowest = float.PositiveInfinity;
                float highestZ = float.NegativeInfinity;
                foreach (var filter in item.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;
                    Bounds bounds = filter.sharedMesh.bounds;
                    Vector3 center = bounds.center;
                    Vector3 extents = bounds.extents;
                    for (int ix = -1; ix <= 1; ix += 2)
                    for (int iy = -1; iy <= 1; iy += 2)
                    for (int iz = -1; iz <= 1; iz += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(ix,iy,iz));
                        Vector3 localCorner = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                        if (localCorner.y < lowest) lowest = localCorner.y;
                        if (localCorner.z > highestZ) highestZ = localCorner.z;
                    }
                }
                if (float.IsInfinity(lowest)) throw new InvalidOperationException("No display mesh bounds: " + prefab);
                Vector3 position = item.localPosition;
                position.y = surfaceY - lowest;
                if (!float.IsNaN(maxBackZ) && highestZ > maxBackZ)
                    position.z -= highestZ - maxBackZ;
                item.localPosition = position;
                return item;
            }
            catch (Exception ex)
            {
                // Optional set dressing must not fall back the persistent shop prefab.
                if (item != null) Object.DestroyImmediate(item.gameObject);
                NamekeeperPlugin.Log.LogWarning("Optional shop display unavailable (" + prefab + "): " + ex.Message);
                return null;
            }
        }

        private static void Lantern(Transform root, float x)
        {
            // Native hanging lantern includes its bracket; face it towards the visitor.
            var lamp = Part(root,"piece_dvergr_lantern",x,2.56f,-1.64f,.60f,.60f,.60f,270, false);
            foreach (var renderer in lamp.GetComponentsInChildren<MeshRenderer>())
            {
                // A light inside a closed lantern must not be occluded by its own glass.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = WorkshopLighting.WarmLanternMaterial(materials[i]);
                renderer.sharedMaterials = materials;
            }
            var light = new GameObject("WorkshopWarmLight");
            light.transform.SetParent(lamp,false);
            light.transform.localPosition = new Vector3(-.952f,-.536f,0);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                WorkshopLighting.Configure(light.AddComponent<Light>());
        }

        private static void Roof(Transform root)
        {
            foreach (float z in new[] { -1.25f,1.25f })
            {
                // Local -Z is the high edge of the installed 26-degree roof prefab.
                Part(root,"darkwood_roof",1.185f,-.04f,z,1.25f,.96f,1.185f,90,roof:true);
                Part(root,"darkwood_roof",-1.185f,-.04f,z,1.25f,.96f,1.185f,270,roof:true);
            }
            Beam(root,new Vector3(0,.93f,0),new Vector3(.28f,.19f,5.15f),true,true);
        }
    }
}
