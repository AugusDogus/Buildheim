using System;

namespace PlanBuild.Client
{
    // Axis-aligned world bounds. Either corner can be the lower one on each axis.
    internal sealed class CaptureBounds
    {
        public float MinX { get; }
        public float MinY { get; }
        public float MinZ { get; }
        public float MaxX { get; }
        public float MaxY { get; }
        public float MaxZ { get; }
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public float Depth => MaxZ - MinZ;
        public bool HasVolume => Width > 0 && Height > 0 && Depth > 0;

        private CaptureBounds(float ax, float ay, float az, float bx, float by, float bz)
        {
            MinX = Math.Min(ax, bx); MinY = Math.Min(ay, by); MinZ = Math.Min(az, bz);
            MaxX = Math.Max(ax, bx); MaxY = Math.Max(ay, by); MaxZ = Math.Max(az, bz);
        }

        public bool Contains(float x, float y, float z) =>
            x >= MinX && x <= MaxX && y >= MinY && y <= MaxY && z >= MinZ && z <= MaxZ;

        public static bool TryCreate(float ax, float ay, float az, float bx, float by, float bz, out CaptureBounds bounds)
        {
            bounds = null;
            foreach (float value in new[] { ax, ay, az, bx, by, bz })
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            var candidate = new CaptureBounds(ax, ay, az, bx, by, bz);
            if (float.IsInfinity(candidate.Width) || float.IsInfinity(candidate.Height) || float.IsInfinity(candidate.Depth)) return false;
            bounds = candidate;
            return true;
        }
    }
}
