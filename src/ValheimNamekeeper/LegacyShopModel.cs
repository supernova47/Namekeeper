using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperNova.ValheimNamekeeper
{
    internal static class LegacyShopModel
    {
        private static Material _wood, _darkWood, _roof, _iron, _brass, _paper, _glow, _signWood;
        private static void PrepareMaterials()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            if (_wood != null) return;
            // Vanilla wood shaders don't always display correctly on Unity's cube mesh.
            // Use repeatable, procedurally textured timber instead of huge tinted slabs.
            var standard = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            if (standard == null)
            {
                var vanilla = PrefabManager.Instance.GetPrefab("woodwall");
                var r = vanilla != null ? vanilla.GetComponentInChildren<MeshRenderer>(true) : null;
                standard = r != null && r.sharedMaterial != null ? r.sharedMaterial.shader : null;
            }
            if (standard == null) return; // Headless server can still own the networked piece.
            _wood = TimberMaterial(standard, new Color(.61f, .39f, .22f), 13);
            _darkWood = TimberMaterial(standard, new Color(.34f, .21f, .13f), 29);
            _roof = TimberMaterial(standard, new Color(.42f, .25f, .14f), 47);
            _iron = SolidMaterial(standard, new Color(.12f, .14f, .15f), .5f);
            _brass = SolidMaterial(standard, new Color(.67f, .43f, .15f), .65f);
            _paper = SolidMaterial(standard, new Color(.79f, .70f, .49f), 0);
            _signWood = TimberMaterial(standard, new Color(.86f, .73f, .51f), 61);
            _glow = WorkshopLighting.GlowMaterial(standard);
        }

        private static Material TimberMaterial(Shader shader, Color tone, int offset)
        {
            var mat = new Material(shader);
            mat.name = "NamekeeperTimber_" + offset;
            var tex = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            tex.name = mat.name + "_Texture";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[96 * 96];
            for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                // Thin wood grain and subtle knots, generated deterministically.
                float grain = Mathf.PerlinNoise((x + offset) * .052f, (y + offset) * .008f);
                float knots = Mathf.PerlinNoise((x + offset) * .022f, (y + offset) * .024f);
                float value = Mathf.Clamp(.72f + (grain - .5f) * .45f + (knots - .5f) * .24f, .40f, 1f);
                pixels[y * 96 + x] = new Color(tone.r * value, tone.g * value, tone.b * value, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            mat.enableInstancing = true;
            mat.SetFloat("_Glossiness", .12f);
            mat.mainTexture = tex;
            mat.color = Color.white;
            return mat;
        }

        internal static void BuildBody(Transform root)
        {
            PrepareMaterials();
            int layer = LayerMask.NameToLayer("piece");
            if (layer < 0) layer = 0;

            // Visual ground plane is deliberately below the placement origin.
            // Long feet keep the shop looking grounded even on uneven terrain.
            for (int i = 0; i < 11; i++)
            {
                float x = -2.25f + i * .45f;
                Block(root, "FloorPlank", new Vector3(x, -.12f, 0f),
                    new Vector3(.425f, .18f, 4.05f), _wood, layer);
            }
            foreach (float x in new[] { -2.05f, 2.05f })
            foreach (float z in new[] { -1.64f, 1.64f })
            {
                Block(root, "FoundationFoot", new Vector3(x, -.39f, z),
                    new Vector3(.36f, .82f, .36f), _darkWood, layer);
                Block(root, "MainPost", new Vector3(x, 1.46f, z),
                    new Vector3(.25f, 3.15f, .25f), _darkWood, layer);
                // Tiny collar visually joins the post with its foundation.
                Block(root, "PostCollar", new Vector3(x, .12f, z),
                    new Vector3(.39f, .12f, .39f), _wood, layer);
            }
            foreach (float z in new[] { -1.52f, 1.52f })
                Block(root, "FloorCrossBeam", new Vector3(0f, -.29f, z),
                    new Vector3(4.7f, .24f, .20f), _darkWood, layer);

            // Rear wall: separate narrow planks, rather than a single flat cube.
            for (int i = 0; i < 12; i++)
            {
                float x = -2.20f + i * .40f;
                Block(root, "BackBoard", new Vector3(x, 1.55f, 1.82f),
                    new Vector3(.385f, 2.85f, .13f), _wood, layer);
            }
            // Half-height side walls leave the shop open to view.
            foreach (float x in new[] { -2.22f, 2.22f })
            {
                for (int i = 0; i < 5; i++)
                    Block(root, "SideRail", new Vector3(x, .32f + i * .33f, .20f),
                        new Vector3(.16f, .31f, 3.18f), _wood, layer);
                Block(root, "SideBeam", new Vector3(x, 1.98f, .10f),
                    new Vector3(.22f, .19f, 3.6f), _darkWood, layer);
            }

            // Counter is near the merchant, with a step-back edge.
            for (int i = 0; i < 10; i++)
                Block(root, "CounterBoard", new Vector3(-1.96f + i * .435f, 1.07f, -1.46f),
                    new Vector3(.415f, .15f, .95f), _wood, layer);
            for (int i = 0; i < 5; i++)
                Block(root, "CounterFrontBoard", new Vector3(0f, .13f + i * .17f, -1.91f),
                    new Vector3(4.1f, .16f, .12f), _darkWood, layer);
            Block(root, "CounterUpperRim", new Vector3(0f, 1.12f, -1.92f),
                new Vector3(4.5f, .17f, .19f), _darkWood, layer);
            Block(root, "CounterBottomShelf", new Vector3(0f, .33f, -1.1f),
                new Vector3(3.8f, .14f, .8f), _wood, layer);

            foreach (float z in new[] { -1.9f, 1.9f })
                Block(root, "RoofCrossBeam", new Vector3(0f, 2.90f, z),
                    new Vector3(4.6f, .16f, .21f), _darkWood, layer);

            // Hanging sign and wood frame, in front and above the merchant.
            Block(root, "SignFace", new Vector3(0f, 2.64f, -1.98f),
                new Vector3(1.85f, .44f, .15f), _signWood, layer);
            Block(root, "SignTop", new Vector3(0f, 2.90f, -1.98f),
                new Vector3(2.06f, .09f, .2f), _darkWood, layer);
            foreach (float x in new[] { -.90f, .90f })
                Block(root, "SignHanger", new Vector3(x, 2.90f, -1.98f),
                    new Vector3(.08f, .45f, .10f), _darkWood, layer);
            Decorate(root, layer);
        }

        private static Transform Block(Transform parent, string name, Vector3 localPos, Vector3 localSize, Material material, int layer, bool roof = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = layer;
            go.tag = roof ? "Untagged" : "leaky";
            var mesh = go.GetComponent<MeshFilter>();
            if (mesh != null) mesh.sharedMesh = WorkshopMesh.Box;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localSize;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                if (material == null) renderer.enabled = false; // dedicated server
            }
            return go.transform;
        }

        internal static void BuildRoof(Transform root)
        {
            PrepareMaterials();
            int layer = LayerMask.NameToLayer("piece");
            if (layer < 0) layer = 0;
            // Roof slope is reversed from 0.1.0: slopes rise toward the ridge.
            // Build multiple adjacent shingles on both sides, with generous eaves.
            for (int i = 0; i < 9; i++)
            {
                float z = -2.24f + i * .56f;
                var left = Block(root, "RoofLeft", new Vector3(-1.14f, .46f, z),
                    new Vector3(2.66f, .16f, .57f), _roof, layer, true);
                left.localRotation = Quaternion.Euler(0f, 0f, 22f);
                var right = Block(root, "RoofRight", new Vector3(1.14f, .46f, z),
                    new Vector3(2.66f, .16f, .57f), _roof, layer, true);
                right.localRotation = Quaternion.Euler(0f, 0f, -22f);
            }
            Block(root, "RoofRidge", new Vector3(0f, .93f, 0f),
                new Vector3(.28f, .19f, 5.15f), _darkWood, layer, true);
            foreach (float z in new[] { -2.48f, 2.48f })
            {
                var left = Block(root, "CarvedGableLeft", new Vector3(-1.14f, .46f, z), new Vector3(2.72f, .20f, .16f), _darkWood, layer, true);
                left.localRotation = Quaternion.Euler(0, 0, 22);
                var right = Block(root, "CarvedGableRight", new Vector3(1.14f, .46f, z), new Vector3(2.72f, .20f, .16f), _darkWood, layer, true);
                right.localRotation = Quaternion.Euler(0, 0, -22);
            }
            foreach (float x in new[] { -2.37f, 2.37f })
                Block(root, "EaveTrim", new Vector3(x, -.04f, 0), new Vector3(.18f, .18f, 5.12f), _darkWood, layer, true);
        }

        private static Material SolidMaterial(Shader shader, Color color, float metallic)
        {
            var material = new Material(shader) { color = color, enableInstancing = true };
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", .22f);
            return material;
        }

        private static Transform Ornament(Transform root, string name, Vector3 position, Vector3 size, Material material, int layer)
        {
            var part = Block(root, name, position, size, material, layer);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static void Decorate(Transform root, int layer)
        {
            // Brace silhouettes and metal collars add depth without widening the old footprint.
            foreach (float x in new[] { -2.05f, 2.05f })
            {
                foreach (float z in new[] { -1.64f, 1.64f })
                {
                    foreach (float y in new[] { .28f, 2.55f })
                        Ornament(root, "IronPostBand", new Vector3(x, y, z), new Vector3(.27f, .09f, .27f), _iron, layer);
                    var brace = Ornament(root, "DiagonalBrace", new Vector3(x - Mathf.Sign(x) * .30f, 2.56f, z), new Vector3(.15f, .88f, .18f), _darkWood, layer);
                    brace.localRotation = Quaternion.Euler(0, 0, Mathf.Sign(x) * -43);
                }
                Lantern(root, new Vector3(x, 2.24f, -1.87f), layer);
            }
            // Carved border and brass studs frame the localized shop sign.
            foreach (float y in new[] { 2.43f, 2.85f })
                Ornament(root, "SignCarving", new Vector3(0, y, -2.072f), new Vector3(1.86f, .035f, .028f), _brass, layer);
            foreach (float x in new[] { -.83f, .83f })
                Ornament(root, "SignStud", new Vector3(x, 2.64f, -2.075f), new Vector3(.07f, .07f, .04f), _brass, layer);
            var plaque = new GameObject("LocalizedWorkshopSign");
            plaque.transform.SetParent(root, false);
            plaque.transform.localPosition = new Vector3(0, 2.64f, -2.059f);
            plaque.AddComponent<WorkshopSign>().Initialize();
            // A leather writing mat, paper, ink, engraving tools and a small display.
            Ornament(root, "WritingMat", new Vector3(.97f, 1.158f, -1.43f), new Vector3(1.02f, .025f, .60f), _darkWood, layer);
            var paper = Ornament(root, "Parchment", new Vector3(.93f, 1.18f, -1.45f), new Vector3(.42f, .022f, .40f), _paper, layer);
            paper.localRotation = Quaternion.Euler(0, 12, 0);
            Ornament(root, "InkPot", new Vector3(1.38f, 1.23f, -1.24f), new Vector3(.13f, .15f, .13f), _iron, layer);
            var quill = Ornament(root, "WritingQuill", new Vector3(1.38f, 1.39f, -1.24f), new Vector3(.035f, .32f, .035f), _paper, layer);
            quill.localRotation = Quaternion.Euler(0, 0, -24);
            Ornament(root, "ToolBoard", new Vector3(-1.02f, 1.90f, 1.69f), new Vector3(1.50f, .65f, .10f), _darkWood, layer);
            for (int i = 0; i < 4; i++)
            {
                float x = -1.54f + i * .34f;
                Ornament(root, "ToolHandle", new Vector3(x, 1.87f, 1.60f), new Vector3(.065f, .38f, .06f), _wood, layer);
                Ornament(root, "ToolHead", new Vector3(x, 2.08f, 1.59f), new Vector3(i % 2 == 0 ? .22f : .09f, .10f, .09f), _iron, layer);
            }
            foreach (float y in new[] { .72f, 1.42f, 2.13f })
                Block(root, "DisplayShelf", new Vector3(1.16f, y, 1.54f), new Vector3(1.48f, .10f, .43f), _darkWood, layer);
            for (int i = 0; i < 3; i++)
            {
                var book = Ornament(root, "Ledger", new Vector3(.74f + i * .32f, 1.56f, 1.48f), new Vector3(.20f, .23f, .28f), _wood, layer);
                book.localRotation = Quaternion.Euler(0, 0, (i - 1) * 8);
                Ornament(root, "LedgerSpine", new Vector3(.74f + i * .32f, 1.56f, 1.32f), new Vector3(.14f, .03f, .025f), _brass, layer);
            }
            Ornament(root, "DisplayBase", new Vector3(-1.18f, 1.22f, -1.40f), new Vector3(.63f, .15f, .45f), _darkWood, layer);
            var blade = Ornament(root, "EngravedBlade", new Vector3(-1.18f, 1.37f, -1.40f), new Vector3(.56f, .055f, .10f), _iron, layer);
            blade.localRotation = Quaternion.Euler(0, 20, 0);
            Ornament(root, "BladeGrip", new Vector3(-1.52f, 1.37f, -1.40f), new Vector3(.22f, .07f, .08f), _wood, layer);
            Ornament(root, "BladeGuard", new Vector3(-1.40f, 1.37f, -1.40f), new Vector3(.06f, .08f, .25f), _brass, layer);
        }

        private static void Lantern(Transform root, Vector3 position, int layer)
        {
            Ornament(root, "LanternHook", position + Vector3.up * .40f, new Vector3(.07f, .40f, .07f), _iron, layer);
            var glass = Ornament(root, "LanternGlow", position, new Vector3(.18f, .28f, .18f), _glow, layer);
            var glassRenderer = glass.GetComponent<MeshRenderer>();
            glassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glassRenderer.receiveShadows = false;
            foreach (float y in new[] { -.18f, .18f })
                Ornament(root, "LanternCap", position + Vector3.up * y, new Vector3(.30f, .07f, .30f), _iron, layer);
            foreach (float x in new[] { -.12f, .12f })
            foreach (float z in new[] { -.12f, .12f })
                Ornament(root, "LanternBar", position + new Vector3(x, 0, z), new Vector3(.025f, .32f, .025f), _iron, layer);
            if (_glow != null)
            {
                var lightObject = new GameObject("WorkshopWarmLight");
                lightObject.transform.SetParent(root, false);
                lightObject.transform.localPosition = position;
                WorkshopLighting.Configure(lightObject.AddComponent<Light>());
            }
        }
    }
}
