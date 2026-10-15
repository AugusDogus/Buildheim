using System.Globalization;
using UnityEngine;

namespace PlanBuild.Blueprints
{
    internal sealed class SnapPointEntry
    {
        public string Line { get; }
        public Vector3 Position { get; }

        private SnapPointEntry(Vector3 position)
        {
            Position = position;
            Line = string.Join(";", position.x.ToString("R", CultureInfo.InvariantCulture),
                position.y.ToString("R", CultureInfo.InvariantCulture), position.z.ToString("R", CultureInfo.InvariantCulture));
        }

        public static bool TryParse(string line, out SnapPointEntry point)
        {
            point = null;
            var parts = line.Replace(',', '.').Split(';');
            if (parts.Length != 3 || !TryCoordinate(parts[0], out var x) ||
                !TryCoordinate(parts[1], out var y) || !TryCoordinate(parts[2], out var z)) return false;
            point = new SnapPointEntry(new Vector3(x, y, z));
            return true;
        }

        private static bool TryCoordinate(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
