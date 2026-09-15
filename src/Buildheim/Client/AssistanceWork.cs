using System;

namespace PlanBuild.Client
{
    // Whether a projection still has anything for the hammer to do.
    //
    // A blueprint you have finished building goes invisible, because Draw skips completed pieces,
    // and untargetable, because Find skips them too. Assistance stayed active regardless, so a
    // finished placement parked the hammer on Repair for ever with nothing on screen to explain it.
    // That was the original report: "it just goes back to the repair hammer".
    //
    // Reads piece state without Unity objects so completion and layer filtering can be tested.
    internal static class AssistanceWork
    {
        public static bool HasWork(int count, Func<int, (bool Completed, float Height)> pieceState,
            BlueprintLayers layers)
        {
            for (int index = 0; index < count; index++)
            {
                var piece = pieceState(index);
                if (!piece.Completed && layers.Contains(piece.Height)) return true;
            }
            return false;
        }
    }
}
