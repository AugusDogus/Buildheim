using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Client
{
    internal sealed class CapturePanel
    {
        private readonly ClientPlanner planner;
        private readonly Text corners, dimensions;
        private readonly Button save;

        public CapturePanel(ClientPlanner planner, Transform parent)
        {
            this.planner = planner;
            PlannerWidgets.Label(parent, "Capture a building", 0, 0, 644, 32, 22, true);
            var name = PlannerWidgets.Input(parent, "Blueprint name", 0, 46, 644);
            PlannerWidgets.Button(parent, "Select / edit box", 0, 100, 314, planner.BeginCapture);
            PlannerWidgets.Button(parent, "Clear selection", 330, 100, 314, planner.Selection.Clear);
            corners = PlannerWidgets.Label(parent, "", 0, 150, 644, 58, 18);
            dimensions = PlannerWidgets.Label(parent, "", 0, 216, 644, 48, 20, true);
            PlannerWidgets.Label(parent, "Pick opposite corners with left and right click. The wheel adjusts the selected corner; Alt changes its height. Middle click switches corners.",
                0, 274, 644, 60, 17);
            PlannerWidgets.Label(parent, "Includes loaded, player-built pieces whose placement point is inside the box. Corner A is the blueprint origin.",
                0, 340, 644, 48, 16);
            save = PlannerWidgets.Button(parent, "Save blueprint", 0, 400, 644, () => planner.Capture(name.text));
        }

        public void Refresh()
        {
            var selection = planner.Selection;
            corners.text = $"A (cyan): {Position(selection.First)}\nB (orange): {Position(selection.Second)}";
            bool ready = selection.TryBounds(out var box) && box.HasVolume;
            int count = ready ? BlueprintLibrary.CapturePieces(box, selection.First.Value).Count : 0;
            dimensions.text = box == null ? "Select two opposite corners to start." :
                $"{box.Width:0.0} × {box.Height:0.0} × {box.Depth:0.0} m | {count} pieces";
            if (box != null && !ready) dimensions.text += "\nGive the box width, height and depth.";
            else if (ready && count == 0) dimensions.text += "\nMove the box around a player-built structure.";
            else if (count > BlueprintDocument.MaxPieces) dimensions.text += $"\nReduce the selection to {BlueprintDocument.MaxPieces} pieces.";
            save.interactable = ready && count > 0 && count <= BlueprintDocument.MaxPieces;
        }

        private static string Position(Vector3? point) => point.HasValue
            ? $"{point.Value.x:0.0}, {point.Value.y:0.0}, {point.Value.z:0.0}" : "not selected";
    }
}
