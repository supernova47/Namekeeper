using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SuperNova.ValheimNamekeeper
{
    internal sealed class WorkshopSign : MonoBehaviour
    {
        private static Mesh _quad;
        private static Material _english, _russian;
        private MeshRenderer _renderer;
        private bool? _wasRussian;
        private float _next;
        private bool _failed;

        internal void Initialize()
        {
            if (_failed || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            try { Refresh(); }
            catch (Exception ex)
            {
                _failed = true;
                var renderer = GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = false;
                NamekeeperPlugin.Log.LogWarning("Workshop sign unavailable (wooden board retained): " + ex.Message);
            }
        }
        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 2;
            Initialize();
        }
        private void Refresh()
        {
            string language = global::Localization.instance != null ? global::Localization.instance.GetSelectedLanguage() : "English";
            bool russian = DialogueLines.UseRussian(language);
            if (_renderer != null && _wasRussian == russian) return;
            // Resolve the complete material before exposing a renderer. Cloned prefabs
            // retain components, but private runtime references are not serialized.
            var material = russian ? (_russian != null ? _russian : (_russian = InkMaterial("ru"))) :
                (_english != null ? _english : (_english = InkMaterial("en")));
            if (_renderer == null)
            {
                var filter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = Quad();
                _renderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
                _renderer.enabled = false;
                _renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderer.receiveShadows = true;
                transform.localScale = new Vector3(1.60f, .40f, 1);
            }
            _renderer.sharedMaterial = material;
            _renderer.enabled = true;
            _wasRussian = russian;
        }
        private static Material InkMaterial(string language)
        {
            // Clone the actual game's Custom/Piece material, including shader and keywords.
            // Opaque wood + baked ink needs neither a guessed shader nor alpha-test variants.
            var source = NativeWorkshopAssets.SignMaterial();
            if (!source.shader.isSupported) throw new InvalidOperationException("Native sign shader is unsupported");
            byte[] bytes;
            using (var stream = typeof(WorkshopSign).Assembly.GetManifestResourceStream("Namekeeper.Assets.sign_" + language + ".png"))
            {
                if (stream == null) throw new InvalidOperationException("Embedded sign texture is missing");
                using (var memory = new MemoryStream()) { stream.CopyTo(memory); bytes = memory.ToArray(); }
            }
            var ink = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(ink, bytes, false)) throw new InvalidOperationException("Invalid sign texture");
            var texture = PaintedWood(source.mainTexture, ink);
            Destroy(ink);
            texture.name = "NamekeeperPaintedWood_" + language;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            var material = new Material(source) { name = "NamekeeperNativeSign_" + language,
                mainTexture = texture, mainTextureScale = Vector2.one, mainTextureOffset = Vector2.zero,
                color = Color.white, enableInstancing = true };
            material.SetFloat("_Glossiness", 0); material.SetFloat("_Metallic", 0);
            material.SetColor("_EmissionColor", Color.black);
            if (material.HasProperty("_Emissive")) material.SetColor("_Emissive", Color.black);
            material.SetInt("_ZWrite", 1);
            // Custom/Piece uses the normal world depth test; keep its native opaque pass.
            material.SetInt("_Cull", (int)CullMode.Back);
            material.DisableKeyword("_EMISSION");
            NamekeeperPlugin.Log.LogInfo("Workshop sign prepared from native material: " + source.shader.name + " / " + language);
            return material;
        }

        private static Texture2D PaintedWood(Texture wood, Texture2D ink)
        {
            var target = RenderTexture.GetTemporary(ink.width, ink.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var texture = new Texture2D(ink.width, ink.height, TextureFormat.RGBA32, true) {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            try
            {
                Graphics.Blit(wood, target);
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,ink.width,ink.height),0,0);
                var background = texture.GetPixels32();
                var letters = ink.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    var timber = (Color)background[i];
                    // Slightly lighten the native timber so dark handwriting is readable.
                    timber = Color.Lerp(timber, new Color(.76f,.62f,.43f), .38f);
                    var pixel = Color.Lerp(timber, (Color)letters[i], letters[i].a / 255f);
                    pixel.a = 1;
                    background[i] = pixel;
                }
                texture.SetPixels32(background);
                texture.Apply(true, true);
                return texture;
            }
            catch { Destroy(texture); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }
        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            _quad = new Mesh { name = "NamekeeperSignInk" };
            // Front faces the visitor (-Z); back faces are culled as well as depth-tested.
            _quad.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0), new Vector3(.5f,-.5f,0) };
            _quad.uv = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
            _quad.triangles = new[] { 0,1,2,0,2,3 }; _quad.RecalculateNormals(); _quad.RecalculateBounds();
            return _quad;
        }
    }
}
