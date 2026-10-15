using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Client
{
    internal static class BlueprintSnapping
    {
        // Translate the whole blueprint so its closest point meets the target, preserving rotation.
        public static bool TryPosition(IReadOnlyList<Vector3> points, Vector3 origin, Quaternion rotation,
            Vector3 target, out Vector3 position)
        {
            position = origin;
            float closest = float.PositiveInfinity;
            bool found = false;
            foreach (var point in points)
            {
                var offset = rotation * point;
                float distance = (origin + offset - target).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                position = target - offset;
                found = true;
            }
            return found;
        }
    }
}
