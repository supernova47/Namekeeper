using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace SuperNova.ValheimNamekeeper
{
    internal static class WorkshopLighting
    {
        private static bool _ready;
        private static Light _native;
        private static float _range, _intensity;
        private static Color _color;
        private static readonly Dictionary<Material, Material> WarmMaterials = new Dictionary<Material, Material>();

        internal static Material WarmLanternMaterial(Material source)
        {
            if (source == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return source;
            if (WarmMaterials.TryGetValue(source, out var cached)) return cached;
            if (!source.HasProperty("_EmissionMap") || source.GetTexture("_EmissionMap") == null) return source;
            Prepare();
            RenderTexture target = null;
            var previous = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                // Preserve the native glass mask while replacing its blue tint with warm light.
                target = RenderTexture.GetTemporary(256,256,0,RenderTextureFormat.ARGB32);
                Graphics.Blit(source.GetTexture("_EmissionMap"),target);
                RenderTexture.active = target;
                texture = new Texture2D(256,256,TextureFormat.RGBA32,true);
                texture.ReadPixels(new Rect(0,0,256,256),0,0);
                var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte value = Math.Max(pixels[i].r, Math.Max(pixels[i].g,pixels[i].b));
                    pixels[i] = new Color32(value,value,value,pixels[i].a);
                }
                texture.SetPixels32(pixels); texture.Apply(true,true);
                var material = new Material(source) { name = "NamekeeperWarmNativeLantern" };
                material.SetTexture("_EmissionMap",texture);
                material.SetColor("_EmissionColor",_color * 2);
                WarmMaterials[source] = material;
                return material;
            }
            catch (Exception ex)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                WarmMaterials[source] = source;
                NamekeeperPlugin.Log.LogWarning("Native lantern tint unavailable: " + ex.Message);
                return source;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
            }
        }
        private static void Prepare()
        {
            if (_ready) return;
            _ready = true;
            try
            {
                var prefab = PrefabManager.Instance.GetPrefab("Lantern_DN");
                if (prefab != null)
                {
                    // Includes inactive equipped/attach visuals; no NPC/item is instantiated.
                    foreach (var candidate in prefab.GetComponentsInChildren<Light>(true))
                        if (candidate.type == LightType.Point && (_native == null || candidate.intensity > _native.intensity)) _native = candidate;
                }
            }
            catch (Exception ex) { NamekeeperPlugin.Log.LogWarning("Lantern_DN lighting reference unavailable: " + ex.Message); }
            _range = LightingRules.Range(_native != null ? _native.range : 0);
            _intensity = LightingRules.Intensity(_native != null ? _native.intensity : 0);
            _color = _native != null && _native.color.maxColorComponent > .1f ? _native.color : new Color(1f,.70f,.38f);
            NamekeeperPlugin.Log.LogInfo("Workshop lantern lighting: " + (_native != null ? "Lantern_DN reference" : "fallback point light") +
                "; range=" + _range + "; intensity=" + _intensity);
        }
        internal static Material GlowMaterial(Shader fallback)
        {
            Prepare();
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? fallback;
            var material = new Material(shader) { name = "NamekeeperOmnidirectionalLanternGlass", color = _color, enableInstancing = true };
            if (shader == fallback)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", _color * 1.7f);
            }
            return material;
        }
        internal static void Configure(Light light)
        {
            Prepare();
            light.type = LightType.Point;
            light.color = _color; light.range = _range; light.intensity = _intensity;
            light.renderMode = LightRenderMode.ForcePixel;
            light.cullingMask = ~LayerMask.GetMask("UI", "ghost");
            light.shadows = _native != null ? _native.shadows : LightShadows.None;
            light.shadowResolution = LightShadowResolution.Low;
            light.enabled = true;
        }
    }

}
