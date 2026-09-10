using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class HammerTarget
    {
        public BlueprintProjection Projection { get; }
        public BlueprintProjection.ProjectedPiece Planned { get; }
        public Player Player { get; }
        public Piece Piece { get; }
        public Vector3 Position => Projection.PiecePosition(Planned);
        public Quaternion Rotation => Projection.PieceRotation(Planned);

        private HammerTarget(BlueprintProjection projection, BlueprintProjection.ProjectedPiece planned, Player player, Piece piece)
        { Projection = projection; Planned = planned; Player = player; Piece = piece; }

        public static HammerTarget Find(BlueprintProjection projection, Player player)
        {
            if (!GameCamera.instance) return null;
            var camera = GameCamera.instance.transform;
            var ray = new Ray(camera.position, camera.forward);
            float closest = 50f;
            if (Physics.Raycast(ray, out var obstacle, closest, player.m_placeRayMask)) closest = obstacle.distance + 0.15f;
            HammerTarget target = null;
            foreach (var planned in projection.Pieces)
            {
                if (planned.Completed) continue;
                var piece = planned.Prefab.GetComponent<Piece>();
                if (!piece || piece.m_repairPiece || piece.m_removePiece) continue;
                // Vanilla placement has no scale input. Scaled imports remain visual guides.
                if (Vector3.Distance(planned.Entry.GetScale(), planned.Prefab.transform.localScale) > 0.01f) continue;
                var matrix = projection.PieceMatrix(planned);
                var inverse = matrix.inverse;
                var localRay = new Ray(inverse.MultiplyPoint3x4(ray.origin), inverse.MultiplyVector(ray.direction));
                if (!planned.LocalBounds.IntersectRay(localRay, out float distance)) continue;
                var hit = matrix.MultiplyPoint3x4(localRay.GetPoint(distance));
                float worldDistance = Vector3.Dot(hit - ray.origin, ray.direction);
                if (worldDistance < 0 || worldDistance >= closest) continue;
                if (Vector3.Distance(player.m_eye.position, projection.PiecePosition(planned)) >=
                    player.m_maxPlaceDistance + piece.m_extraPlacementDistance) continue;
                closest = worldDistance;
                target = new HammerTarget(projection, planned, player, piece);
            }
            return target;
        }

        public bool NearSurface(Vector3 hit)
        {
            var matrix = Projection.PieceMatrix(Planned);
            var localPoint = matrix.inverse.MultiplyPoint3x4(hit);
            return Vector3.Distance(hit, matrix.MultiplyPoint3x4(Planned.LocalBounds.ClosestPoint(localPoint))) <= 0.5f;
        }

        public bool HasInventoryResources()
        {
            var costs = Piece.m_resources.Where(resource => resource.m_resItem)
                .Select(resource => new KeyValuePair<string, int>(resource.m_resItem.m_itemData.m_shared.m_name, resource.GetAmount(0)));
            return BuildMaterials.HasAll(costs, name => Player.GetInventory().CountItems(name));
        }
    }
}
