using UnityEngine;

namespace PlanBuild.Client
{
    // Cached, readable mesh data. Bounds are checked by the caller before testing triangles.
    internal sealed class BlueprintHitTest
    {
        private readonly Vector3[] vertices;
        private readonly int[] triangles;

        public BlueprintHitTest(Vector3[] vertices, int[] triangles)
        { this.vertices = vertices; this.triangles = triangles; }

        public bool Intersect(Ray ray, out float distance)
        {
            distance = float.PositiveInfinity;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var ab = vertices[triangles[i + 1]] - a;
                var ac = vertices[triangles[i + 2]] - a;
                var cross = Vector3.Cross(ray.direction, ac);
                float determinant = Vector3.Dot(ab, cross);
                // Both sides are selectable, including mirrored or upside-down imports.
                if (Mathf.Abs(determinant) < 0.000001f) continue;
                float inverse = 1f / determinant;
                var offset = ray.origin - a;
                float u = Vector3.Dot(offset, cross) * inverse;
                if (u < 0 || u > 1) continue;
                var q = Vector3.Cross(offset, ab);
                float v = Vector3.Dot(ray.direction, q) * inverse;
                if (v < 0 || u + v > 1) continue;
                float hit = Vector3.Dot(ac, q) * inverse;
                if (hit >= 0 && hit < distance) distance = hit;
            }
            return !float.IsPositiveInfinity(distance);
        }
    }
}
