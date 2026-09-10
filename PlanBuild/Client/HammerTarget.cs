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

        public static HammerTarget FromPiece(BlueprintProjection projection, BlueprintProjection.ProjectedPiece planned, Player player)
        {
            if (planned.Completed) return null;
            var piece = planned.Prefab.GetComponent<Piece>();
            if (!piece || piece.m_repairPiece || piece.m_removePiece) return null;
            // Vanilla placement has no scale input. Scaled imports remain visual guides.
            if (Vector3.Distance(planned.Entry.GetScale(), planned.Prefab.transform.localScale) > 0.01f ||
                Vector3.Distance(player.m_eye.position, projection.PiecePosition(planned)) >=
                player.m_maxPlaceDistance + piece.m_extraPlacementDistance) return null;
            return new HammerTarget(projection, planned, player, piece);
        }

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
                var candidate = FromPiece(projection, planned, player);
                if (candidate == null) continue;
                var matrix = projection.PieceMatrix(planned);
                var inverse = matrix.inverse;
                var localRay = new Ray(inverse.MultiplyPoint3x4(ray.origin), inverse.MultiplyVector(ray.direction));
                if (!planned.LocalBounds.IntersectRay(localRay, out float distance)) continue;
                var hit = matrix.MultiplyPoint3x4(localRay.GetPoint(distance));
                float worldDistance = Vector3.Dot(hit - ray.origin, ray.direction);
                if (worldDistance < 0 || worldDistance >= closest) continue;
                closest = worldDistance;
                target = candidate;
            }
            return target;
        }

        public bool TryAutoSurface(bool water, out RaycastHit hit)
        {
            var center = Projection.PieceMatrix(Planned).MultiplyPoint3x4(Planned.LocalBounds.center);
            var ray = new Ray(Player.m_eye.position, center - Player.m_eye.position);
            int mask = water ? Player.m_placeWaterRayMask : Player.m_placeRayMask;
            // Use the first real surface along the line of sight, preserving its normal and type.
            // A wall in the way cannot be skipped to place something behind it.
            return Physics.Raycast(ray, out hit, 50f, mask) && hit.collider && !hit.collider.attachedRigidbody &&
                hit.distance < Player.m_maxPlaceDistance + Piece.m_extraPlacementDistance && NearSurface(hit.point);
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
