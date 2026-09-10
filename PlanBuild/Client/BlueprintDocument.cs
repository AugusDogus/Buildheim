using System;
using System.Collections.Generic;
using System.Linq;
using PlanBuild.Blueprints;

namespace PlanBuild.Client
{
    internal sealed class BlueprintDocument
    {
        public const int MaxPieces = 10000;
        public string Name { get; }
        public IReadOnlyList<PieceEntry> Pieces { get; }

        public BlueprintDocument(string name, IReadOnlyList<PieceEntry> pieces)
        {
            Name = name;
            Pieces = pieces;
        }

        public static bool TryParse(string name, IEnumerable<string> lines, bool vbuild,
            out BlueprintDocument document, out string error)
        {
            document = null;
            error = string.Empty;
            var pieces = new List<PieceEntry>();
            bool readingPieces = true;
            int lineNumber = 0;
            try
            {
                foreach (string raw in lines)
                {
                    lineNumber++;
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#Name:")) name = line.Substring(6).Trim();
                    if (line == "#Pieces") readingPieces = true;
                    if (line == "#SnapPoints" || line == "#Terrain" || line == "#Description") readingPieces = false;
                    if (line.StartsWith("#") || !readingPieces) continue;

                    var entry = vbuild ? PieceEntry.FromVBuild(line) : PieceEntry.FromBlueprint(line);
                    if (string.IsNullOrWhiteSpace(entry.name) || !ValidTransform(entry))
                    {
                        error = $"Invalid piece transform on line {lineNumber}. Fix the file and reload it.";
                        return false;
                    }
                    pieces.Add(entry);
                    if (pieces.Count > MaxPieces)
                    {
                        error = $"Blueprint exceeds the {MaxPieces} piece limit. Split it into smaller blueprints.";
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException || ex is OverflowException)
            {
                error = $"Cannot read piece on line {lineNumber}: {ex.Message} Fix the file and reload it.";
                return false;
            }
            if (pieces.Count == 0)
            {
                error = "This file contains no building pieces. Choose another blueprint.";
                return false;
            }
            document = new BlueprintDocument(name, pieces);
            return true;
        }

        private static bool ValidTransform(PieceEntry entry)
        {
            float[] values = { entry.posX, entry.posY, entry.posZ, entry.rotX, entry.rotY,
                entry.rotZ, entry.rotW, entry.scaleX, entry.scaleY, entry.scaleZ };
            return values.All(x => !float.IsNaN(x) && !float.IsInfinity(x)) &&
                entry.scaleX > 0 && entry.scaleY > 0 && entry.scaleZ > 0;
        }

        public IEnumerable<string> Serialize()
        {
            yield return "#Name:" + Name;
            yield return "#Pieces";
            foreach (var piece in Pieces) yield return piece.line;
        }
    }
}
