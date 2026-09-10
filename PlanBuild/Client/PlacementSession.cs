using System;
using System.Globalization;
using System.Linq;

namespace PlanBuild.Client
{
    internal sealed class PlacementSession
    {
        private string path;
        private string name;
        private string[] blueprint;
        private string savedSignature;
        public bool Active => path != null;

        public bool Open(string directory, long world, long player, out PlacementSave save, out string error)
        {
            path = PlacementStore.PathFor(directory, world, player);
            return PlacementStore.TryRead(path, out save, out error);
        }

        public void SetDocument(BlueprintDocument document)
        {
            name = document.Name;
            blueprint = document.Serialize().ToArray();
            savedSignature = null;
        }

        public bool Save(BlueprintProjection projection, BuildMode mode, MaterialChecklist checklist, out string error)
        {
            error = string.Empty;
            if (!Active || projection == null || blueprint == null) return true;
            var position = projection.Position;
            var checks = checklist.CheckedNames;
            string signature = string.Join("/", new[] { position.x, position.y, position.z, projection.Yaw, projection.Layers.Height }
                .Select(number => number.ToString("R", CultureInfo.InvariantCulture))) +
                $"/{projection.Layers.Selected}/{mode == BuildMode.Guide}/" + SimpleJson.SimpleJson.SerializeObject(checks);
            if (signature == savedSignature) return true;
            var save = new PlacementSave
            {
                Version = 1, Name = name, Blueprint = blueprint,
                X = position.x, Y = position.y, Z = position.z, Yaw = projection.Yaw,
                LayerHeight = projection.Layers.Height, Layer = projection.Layers.Selected,
                PreviewOnly = mode == BuildMode.Guide, CheckedMaterials = checks
            };
            if (!PlacementStore.TryWrite(path, save, out error)) return false;
            savedSignature = signature;
            return true;
        }

        public bool Clear(out string error)
        {
            blueprint = null;
            savedSignature = null;
            error = string.Empty;
            return path == null || PlacementStore.TryClear(path, out error);
        }

        public void Reset() { path = null; blueprint = null; savedSignature = null; }
    }
}
