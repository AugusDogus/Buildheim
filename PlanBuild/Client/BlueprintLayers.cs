using System;
using System.Linq;

namespace PlanBuild.Client
{
    // Bands are measured from the blueprint origin and select whole pieces by their origin height.
    internal sealed class BlueprintLayers
    {
        private readonly float[] origins;
        private int[] available;
        public float Height { get; private set; } = 2f;
        public int? Selected { get; private set; }
        public int Count => available.Length;
        public int Ordinal => Selected.HasValue ? Array.IndexOf(available, Selected.Value) + 1 : 0;

        public BlueprintLayers(System.Collections.Generic.IEnumerable<float> origins)
        {
            this.origins = origins.ToArray();
            available = GetAvailable();
        }

        private int Index(float height) => (int)Math.Floor((double)height / Height);
        private int[] GetAvailable() => origins.Select(Index).Distinct().OrderBy(index => index).ToArray();
        public bool Contains(float originHeight) => !Selected.HasValue || Index(originHeight) == Selected.Value;
        public void All() => Selected = null;
        public void Bottom() { if (available.Length > 0) Selected = available[0]; }
        public bool Select(int? layer)
        {
            if (layer.HasValue && Array.IndexOf(available, layer.Value) < 0) return false;
            Selected = layer;
            return true;
        }

        public void Move(int direction)
        {
            if (available.Length == 0) return;
            if (!Selected.HasValue) { Bottom(); return; }
            int index = Array.IndexOf(available, Selected.Value);
            index = Math.Max(0, Math.Min(available.Length - 1, index + Math.Sign(direction)));
            Selected = available[index];
        }

        public bool SetHeight(float height)
        {
            if (float.IsNaN(height) || float.IsInfinity(height) || height < 0.5f || height > 4f) return false;
            if (height == Height) return true;
            Height = height;
            available = GetAvailable();
            if (Selected.HasValue) Bottom();
            return true;
        }
    }
}
