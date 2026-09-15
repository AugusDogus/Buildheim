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
    // Takes a count and a predicate rather than the pieces themselves, the same shape as
    // AutoBuildQueue, so the rule is testable without Unity or a running world.
    internal static class AssistanceWork
    {
        public static bool HasWork(int count, Func<int, bool> buildable)
        {
            for (int index = 0; index < count; index++)
                if (buildable(index)) return true;
            return false;
        }
    }
}
