using System;

namespace PlanBuild.Client
{
    internal sealed class AutoBuildQueue
    {
        private int nextIndex;
        private int? selectedIndex;
        private float nextAttempt;
        private float nextScan;

        // Keep the upcoming piece visible between attempts, but recheck eligibility every frame.
        public int? Next(int count, float now, Func<int, bool> eligible)
        {
            if (selectedIndex.HasValue && selectedIndex.Value < count && eligible(selectedIndex.Value))
                return selectedIndex;
            selectedIndex = null;
            if (count <= 0 || now < nextScan) return null;
            for (int scanned = 0; scanned < count; scanned++)
            {
                int index = nextIndex % count;
                nextIndex = (index + 1) % count;
                if (!eligible(index)) continue;
                selectedIndex = index;
                nextAttempt = now + 0.5f;
                return index;
            }
            nextScan = now + 0.5f;
            return null;
        }

        public bool TryAttempt(float now)
        {
            if (!selectedIndex.HasValue || now < nextAttempt) return false;
            selectedIndex = null;
            return true;
        }

        public void Reset() { nextIndex = 0; selectedIndex = null; nextAttempt = 0; nextScan = 0; }
    }
}
