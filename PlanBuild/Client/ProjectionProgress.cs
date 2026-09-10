using System.Collections.Generic;
using UnityEngine;

namespace PlanBuild.Client
{
    internal static class ProjectionProgress
    {
        // Index loaded, real pieces so a large blueprint does not scan the world once per entry.
        public static void Refresh(BlueprintProjection projection)
        {
            var cells = new Dictionary<Vector3Int, List<Piece>>();
            foreach (var piece in Piece.s_allPieces)
            {
                if (!piece || !piece.IsPlacedByPlayer()) continue;
                var view = piece.GetComponent<ZNetView>();
                if (!view || !view.IsValid()) continue;
                var cell = Vector3Int.FloorToInt(piece.transform.position);
                if (!cells.TryGetValue(cell, out var bucket)) cells[cell] = bucket = new List<Piece>();
                bucket.Add(piece);
            }
            foreach (var planned in projection.Pieces)
            {
                planned.Completed = false;
                var position = projection.PiecePosition(planned);
                var rotation = projection.PieceRotation(planned);
                var center = Vector3Int.FloorToInt(position);
                for (int x = -1; x <= 1 && !planned.Completed; x++)
                for (int y = -1; y <= 1 && !planned.Completed; y++)
                for (int z = -1; z <= 1 && !planned.Completed; z++)
                {
                    if (!cells.TryGetValue(center + new Vector3Int(x, y, z), out var bucket)) continue;
                    foreach (var actual in bucket)
                    {
                        if (actual.GetComponent<ZNetView>().GetZDO().GetPrefab() != planned.Prefab.name.GetStableHashCode()) continue;
                        if (Vector3.Distance(position, actual.transform.position) > 0.05f ||
                            Quaternion.Angle(rotation, actual.transform.rotation) > 1f ||
                            Vector3.Distance(planned.Entry.GetScale(), actual.transform.lossyScale) > 0.01f) continue;
                        planned.Completed = true;
                        break;
                    }
                }
            }
        }
    }
}
