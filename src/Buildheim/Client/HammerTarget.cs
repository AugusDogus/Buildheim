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
            if (planned.Completed || !projection.Layers.Contains(planned.Entry.posY)) return null;
            var piece = planned.Prefab.GetComponent<Piece>();
            if (!piece || piece.m_repairPiece || piece.m_removePiece) return null;
            // Vanilla placement has no scale input. Scaled imports remain visual guides.
            if (Vector3.Distance(planned.Entry.GetScale(), planned.Prefab.transform.localScale) > 0.01f) return null;
            // A large piece's origin can be far away even when its near edge is within reach.
            // This is only a broad-phase filter; aim and support hits enforce actual reach.
            var matrix = projection.PieceMatrix(planned);
            var near = matrix.MultiplyPoint3x4(planned.LocalBounds.ClosestPoint(
                matrix.inverse.MultiplyPoint3x4(player.m_eye.position)));
            if (Vector3.Distance(player.m_eye.position, near) >=
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
                var pieceMatrix = projection.PieceMatrix(planned);
                foreach (var part in planned.Parts)
                {
                    var matrix = pieceMatrix * part.LocalMatrix;
                    var inverse = matrix.inverse;
                    var localRay = new Ray(inverse.MultiplyPoint3x4(ray.origin), inverse.MultiplyVector(ray.direction));
                    if (!part.Mesh.bounds.IntersectRay(localRay, out float distance)) continue;
                    if (part.HitTest != null && !part.HitTest.Intersect(localRay, out distance)) continue;
                    var hit = matrix.MultiplyPoint3x4(localRay.GetPoint(distance));
                    if (Vector3.Distance(player.m_eye.position, hit) >=
                        player.m_maxPlaceDistance + candidate.Piece.m_extraPlacementDistance) continue;
                    float worldDistance = Vector3.Dot(hit - ray.origin, ray.direction);
                    if (worldDistance < 0 || worldDistance >= closest) continue;
                    closest = worldDistance;
                    target = candidate;
                }
            }
            return target;
        }

        public bool TrySurface(bool water, out RaycastHit hit)
        {
            var matrix = Projection.PieceMatrix(Planned);
            var bounds = Planned.LocalBounds;
            int mask = water ? Player.m_placeWaterRayMask : Player.m_placeRayMask;
            // Probe the piece, not the camera crosshair. Every probe stops at its FIRST
            // real collider, so nearby geometry cannot be used to reach through a wall.
            if (Probe(matrix.MultiplyPoint3x4(bounds.center), mask, out hit)) return true;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var sample = bounds.center;
                    sample[axis] += sign * (bounds.extents[axis] + 0.25f);
                    if (Probe(matrix.MultiplyPoint3x4(sample), mask, out hit)) return true;
                }
            }
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(bounds.extents + Vector3.one * 0.15f, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                if (Probe(matrix.MultiplyPoint3x4(bounds.center + offset), mask, out hit)) return true;
            }
            hit = default;
            return false;
        }

        private bool Probe(Vector3 sample, int mask, out RaycastHit hit)
        {
            var offset = sample - Player.m_eye.position;
            float reach = Player.m_maxPlaceDistance + Piece.m_extraPlacementDistance;
            if (!Physics.Raycast(Player.m_eye.position, offset.normalized, out hit,
                    Mathf.Min(reach, offset.magnitude + 0.5f), mask) ||
                !hit.collider || hit.collider.attachedRigidbody ||
                Vector3.Distance(Player.m_eye.position, hit.point) >= reach || !NearSurface(hit.point)) return false;
            var terrain = hit.collider.GetComponent<Heightmap>();
            var support = hit.collider.GetComponentInParent<Piece>();
            var wear = support ? support.GetComponent<WearNTear>() : null;
            bool water = hit.collider.gameObject.layer == LayerMask.NameToLayer("Water");
            // Select a surface compatible with the piece; the original placement routine
            // still performs all terrain, ward, station, biome and clipping checks.
            return (!(Piece.m_groundOnly || Piece.m_groundPiece || Piece.m_cultivatedGroundOnly) || terrain) &&
                (!Piece.m_waterPiece || water) && (!Piece.m_noInWater || !water) &&
                (!Piece.m_notOnTiltingSurface || hit.normal.y >= 0.8f) &&
                (!Piece.m_inCeilingOnly || hit.normal.y <= -0.5f) &&
                (!Piece.m_notOnFloor || hit.normal.y <= 0.1f) &&
                (!wear || (wear.m_supports && (!Piece.m_notOnWood ||
                    (wear.m_materialType != WearNTear.MaterialType.Wood && wear.m_materialType != WearNTear.MaterialType.HardWood))));
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
