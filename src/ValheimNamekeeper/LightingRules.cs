using System;

namespace SuperNova.ValheimNamekeeper
{
    internal static class LightingRules
    {
        internal static float Range(float native) => Math.Max(6, Math.Min(8, native));
        internal static float Intensity(float native) => native > 0 ? Math.Max(2.5f, Math.Min(4, native)) : 2.5f;
    }
}
