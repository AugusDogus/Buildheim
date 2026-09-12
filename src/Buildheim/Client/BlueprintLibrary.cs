using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlanBuild.Blueprints;
using UnityEngine;

namespace PlanBuild.Client
{
    internal static class BlueprintLibrary
    {
        public static bool TryList(string directory, out string[] files, out string error)
        {
            files = Array.Empty<string>();
            error = string.Empty;
            try
            {
                Directory.CreateDirectory(directory);
                files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Where(x => Path.GetExtension(x).Equals(".blueprint", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetExtension(x).Equals(".vbuild", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                error = $"Cannot list blueprints in {directory}: {ex.Message} Check the directory setting and permissions.";
                return false;
            }
        }

        public static bool TryLoad(string path, out BlueprintDocument document, out string error)
        {
            document = null;
            error = string.Empty;
            try
            {
                if (new FileInfo(path).Length > 8 * 1024 * 1024)
                {
                    error = "Blueprint exceeds 8 MB. Split it into smaller files before loading.";
                    return false;
                }
                return BlueprintDocument.TryParse(Path.GetFileNameWithoutExtension(path), File.ReadLines(path),
                    Path.GetExtension(path).Equals(".vbuild", StringComparison.OrdinalIgnoreCase), out document, out error);
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                error = $"Cannot load {Path.GetFileName(path)}: {ex.Message} Check the file and try again.";
                return false;
            }
        }

        public static List<PieceEntry> CapturePieces(CaptureBounds bounds, Vector3 origin)
        {
            var pieces = new List<PieceEntry>();
            foreach (var piece in Piece.s_allPieces)
            {
                if (!piece || !piece.IsPlacedByPlayer()) continue;
                var position = piece.transform.position;
                if (!bounds.Contains(position.x, position.y, position.z)) continue;
                var view = piece.GetComponent<ZNetView>();
                if (!view || !view.IsValid()) continue;
                var prefab = ZNetScene.instance.GetPrefab(view.GetZDO().GetPrefab());
                if (!prefab || !prefab.GetComponent<Piece>()) continue;
                pieces.Add(new PieceEntry(prefab.name, "Building", position - origin,
                    piece.transform.rotation, string.Empty, piece.transform.lossyScale));
            }
            return pieces;
        }

        public static bool TryCapture(string directory, string name, CaptureBounds bounds, Vector3 origin, out string error)
        {
            error = string.Empty;
            name = name.Trim();
            if (name.Length == 0 || name.Length > 80 || name.Any(c => !char.IsLetterOrDigit(c) && c != ' ' && c != '_' && c != '-'))
            {
                error = "Use a name of 1 to 80 letters, numbers, spaces, underscores or hyphens.";
                return false;
            }
            if (bounds == null || !bounds.HasVolume)
            {
                error = "Select two corners with width, height and depth. Use Alt + wheel to raise the upper corner.";
                return false;
            }
            var pieces = CapturePieces(bounds, origin);
            if (pieces.Count == 0 || pieces.Count > BlueprintDocument.MaxPieces)
            {
                error = $"Found {pieces.Count} pieces. Adjust the box to contain 1 to {BlueprintDocument.MaxPieces} loaded, player-built pieces.";
                return false;
            }
            try
            {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, name + ".blueprint");
                // CreateNew preserves existing blueprints, including when names differ only by case on Windows.
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream))
                {
                    foreach (string line in new BlueprintDocument(name, pieces).Serialize()) writer.WriteLine(line);
                }
                return true;
            }
            catch (Exception ex) when (IsFileError(ex))
            {
                error = $"Cannot save {name}: {ex.Message} Existing files were not overwritten. Choose a new name or check directory permissions.";
                return false;
            }
        }

        private static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
            ex is ArgumentException || ex is NotSupportedException;
    }
}
