using System;

namespace PlanBuild.Client
{
    internal static class BlueprintRotation
    {
        public const float Step = 22.5f;

        // Move to the next grid angle in the requested direction, including old off-grid placements.
        // Call only for an explicit turn; restoring an existing placement must preserve its pose.
        public static float Turn(float yaw, int direction, bool coarse)
        {
            double units = ((yaw % 360 + 360) % 360) / Step;
            int steps = coarse ? 4 : 1;
            double next = direction > 0 ? Math.Floor(units + 0.000001) + steps : Math.Ceiling(units - 0.000001) - steps;
            return (float)((next * Step % 360 + 360) % 360);
        }
    }
}
