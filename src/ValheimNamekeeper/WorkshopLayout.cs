using System;
using System.Collections.Generic;

namespace SuperNova.ValheimNamekeeper
{
    internal readonly struct SnapAnchor
    {
        internal readonly string Key;
        internal readonly float X, Y, Z;
        internal SnapAnchor(string key, float x, float y, float z) { Key = key; X = x; Y = y; Z = z; }
    }

    internal static class WorkshopLayout
    {
        internal const float RoofHeight = 2.90f;
        internal const int RoofWood = 4;
        private static string Direction(int value) => value < 0 ? "negative" : value > 0 ? "positive" : "center";
        internal static IEnumerable<SnapAnchor> BodyAnchors()
        {
            yield return new SnapAnchor("foundation_center", 0, -.58f, 0);
            yield return new SnapAnchor("center", 0, 0, 0);
            foreach (int x in new[] { -1, 0, 1 })
            foreach (int z in new[] { -1, 0, 1 })
                if (x != 0 || z != 0) yield return new SnapAnchor("base_" + Direction(x) + "_" + Direction(z), x * 2.5f, 0, z * 2f);
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                yield return new SnapAnchor("foundation_" + Direction(x) + "_" + Direction(z), x * 2.05f, -.58f, z * 1.64f);
            foreach (var anchor in RoofAnchors())
                yield return new SnapAnchor("mount_" + anchor.Key, anchor.X, RoofHeight, anchor.Z);
        }
        internal static IEnumerable<SnapAnchor> RoofAnchors()
        {
            yield return new SnapAnchor("center", 0, 0, 0);
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                yield return new SnapAnchor("corner_" + Direction(x) + "_" + Direction(z), x * 2.05f, 0, z * 1.64f);
        }
    }

    internal static class RoofMigration
    {
        // 0 = legacy/uninitialized, 1 = creation reserved, 2 = completed.
        // A reserved operation is never retried automatically after a crash/ownership change.
        internal static void Initialize(int state, bool isOwner, bool prefabReady, bool recordedRoofExists,
            Action<int> setState, Action createRoof)
        {
            if (state != 0 || !isOwner) return;
            if (recordedRoofExists) { setState(2); return; }
            if (!prefabReady) return;
            setState(1);
            createRoof();
            setState(2);
        }
        internal static bool CanRefund(bool includedWithShop) => !includedWithShop;
    }
}
