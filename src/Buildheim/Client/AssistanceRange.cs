using UnityEngine;

namespace PlanBuild.Client
{
    // Whether hammer assistance should engage at all, by distance.
    //
    // Without this, the only conditions for assistance to own the hammer were "a placement exists,
    // it is enabled, and a hammer is equipped". A placement is restored enabled on every launch, so
    // one positioned days ago silently parked the hammer on Repair and nulled every other recipe,
    // anywhere in the world, until the player found the placement toggle.
    //
    // Kept pure and free of game state so it can be tested without Unity or a running world.
    internal static class AssistanceRange
    {
        // Slack beyond the blueprint's own extent and the hammer's reach, so assistance does not
        // flicker off while the player steps around the edge of what they are building.
        public const float Margin = 8f;

        // Bounding-sphere radius of one piece measured from the blueprint's origin. The projection
        // only ever rotates about yaw, which preserves distance from that origin, so this is
        // rotation-invariant and needs computing once rather than per frame.
        public static float PieceRadius(Vector3 offset, Bounds localBounds, Vector3 scale)
        {
            float longest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            return offset.magnitude + (localBounds.center.magnitude + localBounds.extents.magnitude) * longest;
        }

        public static bool InRange(Vector3 player, Vector3 projection, float radius, float reach) =>
            Vector3.Distance(player, projection) <= radius + reach + Margin;
    }
}
