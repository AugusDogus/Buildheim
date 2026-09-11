using System;
using System.IO;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace PlanBuild.Client
{
    internal sealed class PlannerWindow : IDisposable
    {
        private enum Tab { Build, Blueprints, Materials, Capture }
        private readonly ClientPlanner planner;
        private GameObject root;
        private GameObject hud;
        private Text hudText;
        private GameObject[] pages;
        private Button[] tabs;
        private Button[] modes;
        private GameObject buildControls;
        private Text buildTitle, modeDescription, layerLabel, positionLabel, status;
        private Text placementToggle, hudToggle;
        private Slider layerHeight;
        private ScrollRect library;
        private MaterialsPanel materials;
        private CapturePanel capture;
        private string listedFiles;
        private float nextRefresh;
        private Tab selected = Tab.Build;
        public bool CaptureVisible => selected == Tab.Capture;
        public void SelectCapture() { if (root) Select(Tab.Capture); else selected = Tab.Capture; }

        public PlannerWindow(ClientPlanner planner) { this.planner = planner; }

        public void Update()
        {
            if (!GUIManager.CustomGUIFront) return;
            if (!root) Create();
            root.SetActive(planner.Visible);
            bool showHud = planner.Config.ShowHud.Value && !planner.Visible && (planner.PlacementEnabled || planner.Selection.Editing) && Player.m_localPlayer.TakeInput();
            hud.SetActive(showHud);
            if (showHud)
            {
                if (planner.Selection.Editing)
                {
                    var selection = planner.Selection;
                    string size = selection.TryBounds(out var box) ? $" | {box.Width:0.0} × {box.Height:0.0} × {box.Depth:0.0} m" : "";
                    hudText.text = $"Capture | adjusting corner {selection.SelectedCorner}{size} | {planner.Config.ToggleKey.Value}: review and save\n{selection.Status}\n{CaptureSelection.Hints}";
                }
                else
                {
                    string mode = ModeName(planner.Mode);
                    string layer = planner.Projection.Layers.Selected.HasValue
                        ? $"Layer {planner.Projection.Layers.Ordinal}/{planner.Projection.Layers.Count}" : "All layers";
                    string detail = planner.Mode == BuildMode.Guide ? "Hologram only. Select and place pieces yourself." : planner.BuildStatus;
                    hudText.text = $"{mode} | {layer} | {planner.Config.ToggleKey.Value}: planner | {planner.Config.AutoBuildKey.Value}: autobuild | " +
                        $"{planner.Config.PlacementKey.Value}: placement | {planner.Config.HudKey.Value}: HUD\n{detail}\n{ProjectionControls.Hints}";
                }
            }
            if (!planner.Visible) return;
            hudToggle.text = $"{(planner.Config.ShowHud.Value ? "Hide HUD" : "Show HUD")} ({planner.Config.HudKey.Value})";
            // Fit within Jotunn's scaled canvas, including small windows and ultrawide screens.
            var canvas = GUIManager.CustomGUIFront.GetComponent<RectTransform>().rect;
            float scale = Mathf.Min(1, Mathf.Min((canvas.width - 32) / 700f, (canvas.height - 32) / 640f));
            root.transform.localScale = Vector3.one * Mathf.Max(0.3f, scale);
            RefreshBuild();
            status.text = planner.Status;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            RefreshLibrary();
            if (selected == Tab.Materials) materials.Refresh();
            if (selected == Tab.Capture) capture.Refresh();
        }

        private void Create()
        {
            var gui = GUIManager.Instance;
            root = gui.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, 700, 640, false);
            root.name = "Buildheim planner";
            PlannerWidgets.Label(root.transform, "Buildheim", 28, 12, 360, 46, 32, true);
            hudToggle = PlannerWidgets.Button(root.transform, "Hide HUD", 396, 20, 164, planner.ToggleHud).GetComponentInChildren<Text>();
            PlannerWidgets.Button(root.transform, "Close", 572, 20, 100, () => planner.SetVisible(false));
            tabs = new Button[4];
            pages = new GameObject[4];
            for (int i = 0; i < tabs.Length; i++)
            {
                Tab tab = (Tab)i;
                tabs[i] = PlannerWidgets.Button(root.transform, tab.ToString(), 28 + i * 164, 70, 152, () => Select(tab));
                pages[i] = PlannerWidgets.Group(root.transform, 28, 124, 644, 442);
            }
            CreateBuild(pages[0].transform);
            CreateLibrary(pages[1].transform);
            materials = new MaterialsPanel(planner, pages[2].transform);
            capture = new CapturePanel(planner, pages[3].transform);
            status = PlannerWidgets.Label(root.transform, "", 28, 578, 644, 46, 16);
            hud = PlannerWidgets.Group(GUIManager.CustomGUIFront.transform, 0, 0, 820, 118);
            var rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 115);
            var background = hud.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.72f);
            background.raycastTarget = false;
            hudText = PlannerWidgets.Label(hud.transform, "", 12, 4, 796, 110, 17);
            hudText.alignment = TextAnchor.MiddleCenter;
            listedFiles = null;
            Select(planner.Projection == null ? Tab.Blueprints : selected);
        }

        private void Select(Tab tab)
        {
            selected = tab;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == (int)tab);
                tabs[i].interactable = i != (int)tab;
            }
            nextRefresh = 0;
        }

        private static string ModeName(BuildMode mode) => mode == BuildMode.Guide ? "Preview only" :
            mode == BuildMode.Automatic ? "Autobuild ON" : "Click to build";

        private void CreateBuild(Transform parent)
        {
            buildTitle = PlannerWidgets.Label(parent, "", 0, 0, 644, 48, 20, true);
            buildControls = PlannerWidgets.Group(parent, 0, 58, 644, 390);
            var body = buildControls.transform;
            modes = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                BuildMode mode = (BuildMode)i;
                modes[i] = PlannerWidgets.Button(body, mode == BuildMode.Automatic ? "Autobuild" : ModeName(mode),
                    i * 216, 0, 210, () => planner.SetMode(mode));
            }
            modeDescription = PlannerWidgets.Label(body, "", 0, 42, 644, 48);
            layerLabel = PlannerWidgets.Label(body, "", 0, 100, 644, 30, 18, true);
            PlannerWidgets.Button(body, "Bottom", 0, 138, 120, () => planner.Projection.Layers.Bottom());
            PlannerWidgets.Button(body, "Previous", 130, 138, 120, () => planner.Projection.Layers.Move(-1));
            PlannerWidgets.Button(body, "Next", 260, 138, 120, () => planner.Projection.Layers.Move(1));
            PlannerWidgets.Button(body, "All layers", 390, 138, 120, () => planner.Projection.Layers.All());
            PlannerWidgets.Label(body, "Layer height", 0, 184, 130, 30);
            layerHeight = PlannerWidgets.Slider(body, 140, 184, 350, 0.5f, 4, 2,
                value => planner.Projection?.Layers.SetHeight(Mathf.Round(value * 2) / 2));
            positionLabel = PlannerWidgets.Label(body, "", 0, 224, 644, 30, 18, true);
            PlannerWidgets.Label(body, ProjectionControls.Hints, 0, 257, 644, 58, 17);
            placementToggle = PlannerWidgets.Button(body, "Disable placement", 0, 336, 210,
                () => planner.SetPlacementEnabled(!planner.PlacementEnabled)).GetComponentInChildren<Text>();
            PlannerWidgets.Button(body, "Move to my feet", 218, 336, 210, planner.MoveToFeet);
            PlannerWidgets.Button(body, "Clear hologram", 436, 336, 208, planner.Clear);
        }

        private void RefreshBuild()
        {
            var projection = planner.Projection;
            buildControls.SetActive(projection != null);
            if (projection == null)
            {
                buildTitle.text = "Choose a building from the Blueprints tab to start.";
                return;
            }
            buildTitle.text = $"{projection.Name}\n{projection.Pieces.Count(piece => piece.Completed)}/{projection.Pieces.Count} built | {projection.MissingPrefabs} unavailable";
            placementToggle.text = planner.PlacementEnabled ? "Disable placement" : "Enable placement";
            for (int i = 0; i < modes.Length; i++) modes[i].interactable = planner.PlacementEnabled && i != (int)planner.Mode;
            modeDescription.text = !planner.PlacementEnabled
                ? $"Placement disabled: hologram and assistance are off. Press {planner.Config.PlacementKey.Value} or Enable placement to resume."
                : planner.Mode == BuildMode.Guide
                ? "Preview only: the hologram is a guide. Select and place hammer pieces yourself."
                : planner.Mode == BuildMode.Automatic
                    ? $"Autobuild ON: walk with your hammer to place nearby pieces. {planner.Config.AutoBuildKey.Value} pauses it."
                    : "Click to build: aim at a hologram piece and click. Its recipe and position are selected for you.";
            var layers = projection.Layers;
            string layer = layers.Selected.HasValue ? $"Layer {layers.Ordinal}/{layers.Count}" : "All layers";
            int total = projection.Pieces.Count(piece => layers.Contains(piece.Entry.posY));
            int built = projection.Pieces.Count(piece => layers.Contains(piece.Entry.posY) && piece.Completed);
            layerLabel.text = $"{layer} | {built}/{total} built | {layers.Height:0.0} m per layer";
            layerHeight.SetValueWithoutNotify(layers.Height);
            positionLabel.text = $"Position hologram | height {projection.Position.y:0.0} m | rotation {projection.Yaw:0.#}°";
        }

        private void CreateLibrary(Transform parent)
        {
            PlannerWidgets.Label(parent, "Choose a blueprint", 0, 0, 330, 30, 22, true);
            PlannerWidgets.Button(parent, "New blueprint", 350, 0, 162, SelectCapture);
            PlannerWidgets.Button(parent, "Refresh", 524, 0, 120, planner.Refresh);
            library = PlannerWidgets.Scroll(parent, 0, 48, 644, 390);
        }

        private void RefreshLibrary()
        {
            string signature = string.Join("\n", planner.Files);
            if (signature == listedFiles) return;
            listedFiles = signature;
            PlannerWidgets.Clear(library.content);
            int row = 0;
            foreach (string file in planner.Files)
            {
                PlannerWidgets.Button(library.content, Path.GetFileNameWithoutExtension(file), 4, row++ * 44 + 4, 612,
                    () => { if (planner.Load(file)) Select(Tab.Build); });
            }
            if (row == 0) PlannerWidgets.Label(library.content, "No blueprints yet. Choose New blueprint to capture a building, or add files to your blueprint folder and refresh.", 8, 8, 592, 90);
            library.content.sizeDelta = new Vector2(0, Math.Max(100, row * 44 + 8));
        }

        public void Hide() { if (root) root.SetActive(false); if (hud) hud.SetActive(false); }
        public void Dispose() { if (root) UnityEngine.Object.Destroy(root); if (hud) UnityEngine.Object.Destroy(hud); }
    }
}
