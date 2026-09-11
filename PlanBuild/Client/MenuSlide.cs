using System;

namespace PlanBuild.Client
{
    internal sealed class MenuSlide
    {
        public float Amount { get; private set; }

        public void Advance(bool visible, float elapsed, float duration)
        {
            if (duration <= 0) { Amount = visible ? 1 : 0; return; }
            float step = Math.Max(0, elapsed) / duration;
            Amount = visible ? Math.Min(1, Amount + step) : Math.Max(0, Amount - step);
        }

        public void Reset() => Amount = 0;
    }
}
