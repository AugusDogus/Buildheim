using System;
using System.Collections.Generic;

namespace PlanBuild.Client
{
    internal static class BuildMaterials
    {
        public static bool HasAll(IEnumerable<KeyValuePair<string, int>> costs, Func<string, int> inventoryCount)
        {
            foreach (var total in Total(costs))
                if (inventoryCount(total.Key) < total.Value) return false;
            return true;
        }

        public static Dictionary<string, long> Missing(IEnumerable<KeyValuePair<string, int>> costs, Func<string, int> inventoryCount)
        {
            var missing = new Dictionary<string, long>();
            foreach (var total in Total(costs))
            {
                long shortage = total.Value - inventoryCount(total.Key);
                if (shortage > 0) missing.Add(total.Key, shortage);
            }
            return missing;
        }

        public static Dictionary<string, long> Total(IEnumerable<KeyValuePair<string, int>> costs)
        {
            var totals = new Dictionary<string, long>();
            foreach (var cost in costs)
            {
                if (cost.Value <= 0) continue;
                totals.TryGetValue(cost.Key, out long amount);
                totals[cost.Key] = amount + cost.Value;
            }
            return totals;
        }
    }
}
