using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Client
{
    internal sealed class MaterialsPanel
    {
        private readonly ClientPlanner planner;
        private readonly ScrollRect list;
        private readonly Text summary;
        private readonly Button scope;
        private bool selectedLayer;
        private string signature;
        private IReadOnlyList<MaterialChecklist.Row> rows = Array.Empty<MaterialChecklist.Row>();

        public MaterialsPanel(ClientPlanner planner, Transform parent)
        {
            this.planner = planner;
            PlannerWidgets.Label(parent, "Materials for unfinished pieces", 0, 0, 644, 32, 22, true);
            scope = PlannerWidgets.Button(parent, "Whole blueprint", 0, 44, 210,
                () => { selectedLayer = !selectedLayer; signature = null; Refresh(); });
            summary = PlannerWidgets.Label(parent, "", 224, 40, 420, 48, 16);
            PlannerWidgets.Label(parent, "Material", 48, 100, 218, 28, 17, true);
            PlannerWidgets.Label(parent, "Need", 276, 100, 76, 28, 17, true);
            PlannerWidgets.Label(parent, "Bag", 360, 100, 70, 28, 17, true);
            PlannerWidgets.Label(parent, "Chests", 444, 100, 82, 28, 17, true);
            PlannerWidgets.Label(parent, "Gather", 538, 100, 80, 28, 17, true);
            list = PlannerWidgets.Scroll(parent, 0, 132, 644, 196);
            PlannerWidgets.Button(parent, "Check stocked", 0, 340, 154, () => Check(rows.Where(row => row.Missing == 0)));
            PlannerWidgets.Button(parent, "Check all", 164, 340, 144, () => Check(rows));
            PlannerWidgets.Button(parent, "Reset checks", 318, 340, 154, () => { planner.Materials.ClearChecks(); Refresh(); });
            PlannerWidgets.Button(parent, "Forget chests", 482, 340, 162, () => { planner.ForgetChests(); Refresh(); });
            PlannerWidgets.Label(parent, "Open chests to record their contents. Counts can become stale.\nCheckmarks are gathering notes. Building uses your inventory only.", 0, 388, 644, 52, 16);
        }

        private void Check(IEnumerable<MaterialChecklist.Row> selection) { planner.Materials.CheckAll(selection); Refresh(); }

        public void Refresh()
        {
            rows = planner.MaterialRows(selectedLayer);
            scope.GetComponentInChildren<Text>().text = selectedLayer ? "Selected layer" : "Whole blueprint";
            int unavailable = planner.Projection?.MissingPrefabs ?? 0;
            summary.text = $"{planner.Materials.ChestCount} chests observed this session" +
                (unavailable > 0 ? $"\n{unavailable} unavailable pieces excluded" : "\nBag is live. Chests show last known contents.");
            string next = (planner.Projection == null ? "empty:" : "loaded:") +
                string.Join("\n", rows.Select(row => $"{row.Name}:{row.Required}:{row.Carried}:{row.Stored}:{row.Checked}"));
            if (next == signature) return;
            signature = next;
            PlannerWidgets.Clear(list.content);
            int i = 0;
            foreach (var row in rows.OrderBy(row => Localization.instance.Localize(row.Name), StringComparer.CurrentCultureIgnoreCase))
            {
                float y = i++ * 40 + 2;
                PlannerWidgets.Button(list.content, row.Checked ? "✓" : "", 4, y, 34,
                    () => { planner.Materials.Check(row.Name, !row.Checked); Refresh(); }, 32);
                PlannerWidgets.Label(list.content, Localization.instance.Localize(row.Name), 48, y, 216, 34, 17);
                PlannerWidgets.Label(list.content, row.Required.ToString(), 276, y, 76, 34, 17);
                PlannerWidgets.Label(list.content, row.Carried.ToString(), 360, y, 70, 34, 17);
                PlannerWidgets.Label(list.content, row.Stored.ToString(), 444, y, 82, 34, 17);
                var missing = PlannerWidgets.Label(list.content, row.Missing.ToString(), 538, y, 80, 34, 17);
                if (row.Missing == 0) missing.color = new Color(0.65f, 0.9f, 0.5f);
            }
            if (rows.Count == 0) PlannerWidgets.Label(list.content,
                planner.Projection == null ? "Load a blueprint to see its materials." : "No remaining material requirements in this view.", 8, 8, 592, 80);
            list.content.sizeDelta = new Vector2(0, Math.Max(90, i * 40 + 4));
        }
    }
}
