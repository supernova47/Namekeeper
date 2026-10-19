using System;

namespace SuperNova.ValheimNamekeeper
{
    internal readonly struct HudLayout
    {
        internal readonly float Width, Height, Scale, LeftInset;
        internal HudLayout(float availableWidth, float availableHeight)
        {
            // Keep the 1080p design proportions instead of growing to fill smaller screens.
            // All dimensions are in the parent's canvas units, including its existing scaling.
            Width = 940;
            Height = 760;
            Scale = Math.Min(1, Math.Min(Math.Max(1, availableWidth) / 1920,
                Math.Max(1, availableHeight) / 1080));
            LeftInset = 24 * Scale;
        }
    }
}
