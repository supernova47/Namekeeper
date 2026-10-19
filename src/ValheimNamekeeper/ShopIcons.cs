using System;
using Jotunn.Managers;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal static class ShopIcons
    {
        internal static void Assign(GameObject prefab, bool roof)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var piece = prefab.GetComponent<Piece>();
            try
            {
                // Keep the native config icon if custom texture creation fails.
                piece.m_icon = Fallback(roof);
                var request = new RenderManager.RenderRequest(prefab)
                {
                    Width = 256, Height = 256, Rotation = Quaternion.Euler(15, -35, 0), UseCache = false
                };
                var sprite = RenderManager.Instance.Render(request);
                if (sprite != null) piece.m_icon = sprite;
                else NamekeeperPlugin.Log.LogWarning("Workshop icon render unavailable; keeping custom silhouette");
            }
            catch (Exception ex) { NamekeeperPlugin.Log.LogWarning("Workshop icon fallback: " + ex.Message); }
        }

        // Always a distinct workshop icon even if a graphics driver cannot render the prefab.
        private static Sprite Fallback(bool roofOnly)
        {
            const int size = 128;
            var colors = new Color[size * size];
            void Rect(int x, int y, int w, int h, Color c)
            {
                for (int j = Mathf.Max(0,y); j < Mathf.Min(size,y+h); j++)
                for (int i = Mathf.Max(0,x); i < Mathf.Min(size,x+w); i++) colors[j*size+i] = c;
            }
            var wood = new Color(.58f,.36f,.18f); var dark = new Color(.27f,.16f,.09f); var gold = new Color(.91f,.66f,.27f);
            if (!roofOnly)
            {
                Rect(20,16,88,7,dark); Rect(26,23,8,63,dark); Rect(94,23,8,63,dark);
                Rect(34,25,60,32,wood); Rect(20,55,88,7,gold); Rect(44,70,40,12,wood);
                Rect(30,65,5,10,gold); Rect(93,65,5,10,gold);
            }
            for (int x=12; x<116; x++)
            {
                int peak = 110 - Mathf.Abs(x-64)/2;
                Rect(x,peak-10,1,10,wood); Rect(x,peak-12,1,3,dark);
            }
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) { name = roofOnly ? "NamekeeperRoofIcon" : "NamekeeperShopIcon" };
            texture.SetPixels(colors); texture.Apply();
            return Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f), 100);
        }
    }
}
