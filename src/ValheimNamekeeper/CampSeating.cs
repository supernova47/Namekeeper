using System;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    internal static class CampSeating
    {
        internal static void Install(Transform bench)
        {
            var source = NativeWorkshopAssets.Source("piece_bench01");
            foreach (var seat in source.GetComponentsInChildren<Chair>(true))
            {
                // Geometry copying keeps each seat's collider but drops its empty
                // attachment transform. Recreate that transform in the original frame.
                var target = bench.Find(seat.transform.name);
                if (target == null || seat.m_attachPoint == null)
                    throw new InvalidOperationException("Native bench seat is incomplete: " + seat.name);
                var attach = new GameObject("attachpoint").transform;
                attach.SetParent(target, false);
                attach.localPosition = seat.transform.InverseTransformPoint(seat.m_attachPoint.position);
                attach.localRotation = Quaternion.Inverse(seat.transform.rotation) * seat.m_attachPoint.rotation;
                var chair = target.gameObject.AddComponent<Chair>();
                chair.m_name = seat.m_name;
                chair.m_useDistance = seat.m_useDistance;
                chair.m_attachPoint = attach;
                chair.m_detachOffset = seat.m_detachOffset;
                chair.m_attachAnimation = seat.m_attachAnimation;
                chair.m_hoverOffset = seat.m_hoverOffset;
                chair.m_inShip = false;
            }
        }
    }
}
