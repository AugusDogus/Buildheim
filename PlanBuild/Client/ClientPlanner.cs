using System;
using System.IO;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class ClientPlanner : IDisposable
    {
        private readonly ClientConfig config;
        private readonly HammerAssistance assistance = new HammerAssistance();
        private BuildMode buildMode = BuildMode.Assisted;
        private float nextProgressCheck;
        private string[] files = Array.Empty<string>();
        private BlueprintProjection projection;
        private ZNetScene scene;
        private bool visible;
        private Rect window = new Rect(30, 80, 420, 620);
        private Vector2 scroll;
        private Vector2 contentScroll;
        private GUIStyle statusStyle;
        private string captureName = "My build";
        private float radius = 15f;
        private string status = "Load a blueprint or capture nearby buildings.";

        public ClientPlanner(ClientConfig config) { this.config = config; }

        public void Update()
        {
            if (scene != ZNetScene.instance)
            {
                Clear();
                SetVisible(false);
                scene = ZNetScene.instance;
            }
            if (!Player.m_localPlayer || Player.m_localPlayer.IsDead())
            { buildMode = BuildMode.Assisted; SetVisible(false); assistance.SetProjection(null); return; }
            UpdateAssistance();
            if (projection != null && Time.time >= nextProgressCheck)
            {
                ProjectionProgress.Refresh(projection);
                nextProgressCheck = Time.time + 0.5f;
            }
            if (Settings.instance && Settings.instance.isActiveAndEnabled) return;
            if (Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus())) return;
            if (!visible && projection != null && Player.m_localPlayer.TakeInput() && Input.GetKeyDown(config.AutoBuildKey.Value))
            {
                buildMode = buildMode == BuildMode.Automatic ? BuildMode.Assisted : BuildMode.Automatic;
                UpdateAssistance();
            }
            if (Input.GetKeyDown(config.ToggleKey.Value))
            {
                if (!visible) Refresh();
                SetVisible(!visible);
            }
            if (visible && Input.GetKeyDown(KeyCode.Escape)) SetVisible(false);
        }

        private void SetVisible(bool value)
        {
            if (visible == value) return;
            visible = value;
            UpdateAssistance();
            GUIManager.BlockInput(value);
            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.m_placePressedTime = -9999f;
                Player.m_localPlayer.m_removePressedTime = -9999f;
                ZInput.ResetButtonStatus("Attack");
                ZInput.ResetButtonStatus("JoyPlace");
            }
        }

        private void UpdateAssistance() => assistance.SetProjection(!visible && buildMode != BuildMode.Guide ? projection : null, buildMode);

        public void DrawProjection()
        {
            if (Player.m_localPlayer && scene == ZNetScene.instance) projection?.Draw(assistance.SelectedPiece);
        }

        public void DrawWindow()
        {
            if (!Player.m_localPlayer) return;
            statusStyle ??= new GUIStyle(GUI.skin.box) { wordWrap = true };
            if (!visible)
            {
                if (projection != null && buildMode != BuildMode.Guide)
                {
                    string layer = projection.Layers.Selected.HasValue
                        ? $"Layer {projection.Layers.Ordinal}/{projection.Layers.Count}" : "All layers";
                    string message = buildMode == BuildMode.Automatic
                        ? $"Autobuild ON ({config.AutoBuildKey.Value} to pause) | {layer}\n{assistance.Status}"
                        : $"{layer}\n{assistance.Status}";
                    GUI.Box(new Rect(Screen.width / 2f - 300, Screen.height - 100, 600, 50), message, statusStyle);
                }
                return;
            }
            window.x = Mathf.Clamp(window.x, 0, Mathf.Max(0, Screen.width - window.width));
            window.height = Mathf.Min(620, Screen.height - 40);
            window.y = Mathf.Clamp(window.y, 0, Mathf.Max(0, Screen.height - window.height));
            window = GUILayout.Window(PlanBuildPlugin.PluginGUID.GetHashCode(), window, WindowContents, "PlanBuild | Private blueprints");
        }

        private void WindowContents(int id)
        {
            contentScroll = GUILayout.BeginScrollView(contentScroll, GUILayout.Height(window.height - 65));
            GUILayout.Label("Capture player-built pieces around your current position.");
            captureName = GUILayout.TextField(captureName, 80);
            GUILayout.Label($"Capture radius: {radius:0} m");
            radius = Mathf.Round(GUILayout.HorizontalSlider(radius, 2, 50));
            if (GUILayout.Button("Capture and save"))
            {
                if (BlueprintLibrary.TryCapture(config.Directory.Value, captureName, Player.m_localPlayer, radius, out status))
                { Refresh(); status = $"Saved {captureName}.blueprint. Your position is its origin."; }
            }
            if (GUILayout.Button("Refresh local blueprints")) Refresh();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(160));
            foreach (string file in files)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(file))) Load(file);
            }
            GUILayout.EndScrollView();
            if (projection != null)
            {
                GUILayout.Label($"{projection.Name}: {projection.Pieces.Count(x => x.Completed)}/{projection.Pieces.Count} built ({projection.MissingPrefabs} unavailable)");
                int selectedMode = GUILayout.SelectionGrid((int)buildMode, new[] { "Guide", "Click to build", "Autobuild" }, 3);
                buildMode = selectedMode == 2 ? BuildMode.Automatic : selectedMode == 1 ? BuildMode.Assisted : BuildMode.Guide;
                if (buildMode == BuildMode.Automatic) GUILayout.Label($"Walk with your hammer equipped to build. {config.AutoBuildKey.Value} pauses autobuild.");
                DrawLayers();
                GUILayout.Label("Position the hologram, then close this window to build.");
                if (GUILayout.Button("Move origin to my feet")) projection.Position = Player.m_localPlayer.transform.position;
                MoveButtons("East / west", Vector3.right);
                MoveButtons("Up / down", Vector3.up);
                MoveButtons("North / south", Vector3.forward);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rotate -22.5")) projection.Yaw -= 22.5f;
                if (GUILayout.Button("Rotate +22.5")) projection.Yaw += 22.5f;
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Clear hologram")) Clear();
            }
            GUILayout.Label(status);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close")) SetVisible(false);
            GUI.DragWindow(new Rect(0, 0, window.width, 25));
        }

        private void MoveButtons(string label, Vector3 axis)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            float step = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.1f;
            if (GUILayout.Button($"-{step:0.0} m")) projection.Position -= axis * step;
            if (GUILayout.Button($"+{step:0.0} m")) projection.Position += axis * step;
            GUILayout.EndHorizontal();
        }

        private void DrawLayers()
        {
            var layers = projection.Layers;
            GUILayout.Label($"Layer height: {layers.Height:0.0} m");
            float height = Mathf.Round(GUILayout.HorizontalSlider(layers.Height, 0.5f, 4f) * 2f) / 2f;
            layers.SetHeight(height);
            if (layers.Selected is int index)
            {
                int total = projection.Pieces.Count(piece => layers.Contains(piece.Entry.posY));
                int built = projection.Pieces.Count(piece => layers.Contains(piece.Entry.posY) && piece.Completed);
                GUILayout.Label($"Layer {layers.Ordinal}/{layers.Count}: {index * layers.Height:0.0} to {(index + 1) * layers.Height:0.0} m | {built}/{total} built");
            }
            else GUILayout.Label("All layers visible and available to build");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bottom")) layers.Bottom();
            if (GUILayout.Button("Previous")) layers.Move(-1);
            if (GUILayout.Button("Next")) layers.Move(1);
            if (GUILayout.Button("All layers")) layers.All();
            GUILayout.EndHorizontal();
        }

        private void Refresh()
        {
            if (BlueprintLibrary.TryList(config.Directory.Value, out var found, out status)) files = found;
        }

        private void Load(string file)
        {
            if (!BlueprintLibrary.TryLoad(file, out var document, out status) ||
                !BlueprintProjection.TryCreate(document, out var next, out status)) return;
            Clear();
            projection = next;
            projection.Position = Player.m_localPlayer.transform.position;
            status = "Loaded locally. Terrain instructions and container contents are not applied.";
        }

        private void Clear() { buildMode = BuildMode.Assisted; assistance.SetProjection(null); projection?.Dispose(); projection = null; }
        public void Dispose() { Clear(); SetVisible(false); assistance.Dispose(); }
    }
}
