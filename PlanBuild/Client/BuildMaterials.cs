using System;
using System.Collections.Generic;

namespace PlanBuild.Client
{
    internal static class BuildMaterials
    {
        public static bool HasAll(IEnumerable<KeyValuePair<string, int>> costs, Func<string, int> inventoryCount)
        {
            var totals = new Dictionary<string, long>();
            foreach (var cost in costs)
            {
                if (cost.Value <= 0) continue;
                totals.TryGetValue(cost.Key, out long amount);
                totals[cost.Key] = amount + cost.Value;
            }
            foreach (var total in totals)
                if (inventoryCount(total.Key) < total.Value) return false;
            return true;
        }
    }
}
