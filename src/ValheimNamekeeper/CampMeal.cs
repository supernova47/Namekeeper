using System;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    // Hildir's bowl and its turnips are siblings, not a single prefab. Their
    // authored transforms must stay in the SAME frame, including the bowl scale.
    internal static class CampMeal
    {
        private const string BowlKey = "SN_HildirMealBowl";
        private const string FoodKey = "SN_HildirMealTurnips";

        internal static void Capture(GameObject nativeCamp)
        {
            var bowl = nativeCamp.transform.Find("fi_vil_container_bucket01");
            var food = nativeCamp.transform.Find("turnips");
            if (bowl == null || food == null || bowl.parent != food.parent)
                throw new InvalidOperationException("Hildir's authored bowl/turnips group is unavailable");
            NativeWorkshopAssets.Remember(BowlKey, bowl.gameObject);
            NativeWorkshopAssets.Remember(FoodKey, food.gameObject);
        }

        internal static Transform Create(Transform camp, Vector3 ground)
        {
            var nativeBowl = NativeWorkshopAssets.Source(BowlKey).transform;
            var nativeFood = NativeWorkshopAssets.Source(FoodKey).transform;
            var root = new GameObject("HildirCampMeal");
            root.SetActive(false);
            root.transform.SetParent(camp, false);
            try
            {
                // Rotate/translate the WHOLE composition only. The original bowl's
                // longer Z axis becomes the feeding row's X axis in our camp.
                root.transform.localRotation = Quaternion.Euler(0, 90f, 0) * Quaternion.Inverse(nativeBowl.localRotation);
                var frame = new GameObject("NativeMealFrame").transform;
                frame.SetParent(root.transform, false);
                frame.localPosition = -nativeBowl.localPosition;
                var bowl = Copy(frame, BowlKey, nativeBowl, true);
                Copy(frame, FoodKey, nativeFood, false);

                // Ground by the BOWL's bottom, never by the food/foliage bounds.
                Bounds bounds = WorldCamp.MeshBoundsIn(bowl, camp);
                Vector3 destination = camp.InverseTransformPoint(ground);
                root.transform.localPosition += destination -
                    new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                root.SetActive(true);
                return root.transform;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        private static Transform Copy(Transform frame, string key, Transform source, bool solid)
        {
            var part = NativeWorkshopAssets.Add(frame, key, source.localPosition, source.localScale, 0, solid);
            part.localRotation = source.localRotation;
            return part;
        }
    }
}
