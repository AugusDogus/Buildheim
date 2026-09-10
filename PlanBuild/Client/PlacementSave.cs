using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace PlanBuild.Client
{
    // Only local planning data is saved. Real pieces are always rediscovered from the world.
    internal sealed class PlacementSave
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public string[] Blueprint { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float LayerHeight { get; set; }
        public int? Layer { get; set; }
        public bool PreviewOnly { get; set; }
        public string[] CheckedMaterials { get; set; }

        public bool IsValid() => Version == 1 && !string.IsNullOrWhiteSpace(Name) && Name.Length <= 512 &&
            Blueprint != null && Blueprint.Length > 0 && Blueprint.Length <= BlueprintDocument.MaxPieces + 2 &&
            Blueprint.All(line => line != null) && new[] { X, Y, Z, Yaw, LayerHeight }.All(Finite) &&
            LayerHeight >= 0.5f && LayerHeight <= 4f && CheckedMaterials != null && CheckedMaterials.Length <= 10000 &&
            CheckedMaterials.All(name => !string.IsNullOrWhiteSpace(name) && name.Length <= 512);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal static class PlacementStore
    {
        public static string PathFor(string directory, long world, long player) =>
            Path.Combine(directory, $"{world}-{player}.json");

        public static bool TryRead(string path, out PlacementSave save, out string error)
        {
            save = null;
            error = string.Empty;
            try
            {
                if (!File.Exists(path)) return false;
                if (new FileInfo(path).Length > 8 * 1024 * 1024)
                { error = "Saved hologram exceeds 8 MB. Load a smaller blueprint manually; the save was preserved."; return false; }
                var parsed = SimpleJson.SimpleJson.DeserializeObject<PlacementSave>(File.ReadAllText(path));
                if (parsed == null || !parsed.IsValid() ||
                    !BlueprintDocument.TryParse(parsed.Name, parsed.Blueprint, false, out _, out _))
                { error = "Saved hologram is invalid or uses an unsupported format. Load a blueprint manually; the save was preserved."; return false; }
                save = parsed;
                return true;
            }
            catch (Exception ex) when (Expected(ex))
            {
                error = $"Cannot restore the hologram: {ex.Message} The save was preserved. Load a blueprint manually or check the placement file.";
                return false;
            }
        }

        public static bool TryWrite(string path, PlacementSave save, out string error)
        {
            error = string.Empty;
            if (!save.IsValid()) { error = "The hologram could not be saved because its planning data is invalid."; return false; }
            string pending = path + ".tmp";
            try
            {
                string json = SimpleJson.SimpleJson.SerializeObject(save);
                if (System.Text.Encoding.UTF8.GetByteCount(json) > 8 * 1024 * 1024)
                { error = "This hologram is too large to save. Reduce the blueprint size before leaving the world."; return false; }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(pending, FileMode.Create, FileAccess.Write))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(pending, path, null);
                else File.Move(pending, path);
                return true;
            }
            catch (Exception ex) when (Expected(ex))
            {
                error = $"Cannot save hologram to {path}: {ex.Message} The previous save is preserved. Check disk space and permissions before leaving.";
                return false;
            }
        }

        public static bool TryClear(string path, out string error)
        {
            error = string.Empty;
            try { File.Delete(path); return true; }
            catch (Exception ex) when (Expected(ex))
            { error = $"Cannot remove the saved hologram: {ex.Message} It may return when you reconnect. Check permissions for {path}."; return false; }
        }

        private static bool Expected(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
            ex is ArgumentException || ex is SerializationException || ex is InvalidCastException || ex is FormatException ||
            ex is OverflowException || ex is NotSupportedException;
    }
}
