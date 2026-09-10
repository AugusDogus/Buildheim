using System;

namespace PlanBuild.Client
{
    internal sealed class AutoBuildQueue
    {
        private int nextIndex;
        private float nextAttempt;

        public int? Next(int count, float now, Func<int, bool> eligible)
        {
            if (count <= 0 || now < nextAttempt) return null;
            nextAttempt = now + 0.5f;
            for (int scanned = 0; scanned < count; scanned++)
            {
                int index = nextIndex % count;
                nextIndex = (index + 1) % count;
                if (eligible(index)) return index;
            }
            return null;
        }

        public void Reset() { nextIndex = 0; nextAttempt = 0; }
    }
}
