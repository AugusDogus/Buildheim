using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanBuild.Client
{
    // Gathering notes are separate from inventory ownership and never authorize placement.
    internal sealed class MaterialChecklist
    {
        internal sealed class Row
        {
            public string Name { get; }
            public long Required { get; }
            public long Carried { get; }
            public long Stored { get; }
            public long Missing => Math.Max(0, Required - Carried - Stored);
            public bool Checked { get; }
            public Row(string name, long required, long carried, long stored, bool marked)
            { Name = name; Required = required; Carried = carried; Stored = stored; Checked = marked; }
        }

        private readonly Dictionary<string, Dictionary<string, long>> chests = new Dictionary<string, Dictionary<string, long>>();
        private readonly HashSet<string> marked = new HashSet<string>();
        public int ChestCount => chests.Count;
        public string[] CheckedNames => marked.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        public void Observe(string chest, IEnumerable<KeyValuePair<string, int>> items) => chests[chest] = BuildMaterials.Total(items);
        public void ForgetChests() => chests.Clear();
        public void ClearChecks() => marked.Clear();
        public void Check(string name, bool value) { if (value) marked.Add(name); else marked.Remove(name); }
        public void CheckAll(IEnumerable<Row> rows) { foreach (var row in rows) marked.Add(row.Name); }

        public IReadOnlyList<Row> Rows(IEnumerable<KeyValuePair<string, int>> requirements, IEnumerable<KeyValuePair<string, int>> inventory)
        {
            var carried = BuildMaterials.Total(inventory);
            return BuildMaterials.Total(requirements).Select(cost =>
            {
                carried.TryGetValue(cost.Key, out long bag);
                long stored = 0;
                foreach (var chest in chests.Values)
                    if (chest.TryGetValue(cost.Key, out long count)) stored += count;
                return new Row(cost.Key, cost.Value, bag, stored, marked.Contains(cost.Key));
            }).OrderBy(row => row.Name, StringComparer.Ordinal).ToArray();
        }
    }
}
