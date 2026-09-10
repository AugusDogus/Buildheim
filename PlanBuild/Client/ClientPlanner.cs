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
        private bool assistBuilding = true;
        private float nextProgressCheck;
        private string[] files = Array.Empty<string>();
        private BlueprintProjection projection;
        private ZNetScene scene;
        private bool visible;
        private Rect window = new Rect(30, 80, 420, 620);
        private Vector2 scroll;
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
            { SetVisible(false); assistance.SetProjection(null); return; }
            assistance.SetProjection(!visible && assistBuilding ? projection : null);
            if (projection != null && Time.time >= nextProgressCheck)
            {
                ProjectionProgress.Refresh(projection);
                nextProgressCheck = Time.time + 0.5f;
            }
            if (Settings.instance && Settings.instance.isActiveAndEnabled) return;
            if (Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus())) return;
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
            assistance.SetProjection(!visible && assistBuilding ? projection : null);
            GUIManager.BlockInput(value);
        }

        public void DrawProjection()
        {
            if (Player.m_localPlayer && scene == ZNetScene.instance) projection?.Draw(assistance.SelectedPiece);
        }

        public void DrawWindow()
        {
            if (!Player.m_localPlayer) return;
            if (!visible)
            {
                if (projection != null && assistBuilding)
                    GUI.Box(new Rect(Screen.width / 2f - 300, Screen.height - 100, 600, 50), assistance.Status);
                return;
            }
            window.x = Mathf.Clamp(window.x, 0, Mathf.Max(0, Screen.width - window.width));
            window.y = Mathf.Clamp(window.y, 0, Mathf.Max(0, Screen.height - window.height));
            window = GUILayout.Window(PlanBuildPlugin.PluginGUID.GetHashCode(), window, WindowContents, "PlanBuild | Private blueprints");
        }

        private void WindowContents(int id)
        {
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
                assistBuilding = GUILayout.Toggle(assistBuilding, "Assist hammer: select and align the piece I aim at");
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

        private void Clear() { assistance.SetProjection(null); projection?.Dispose(); projection = null; }
        public void Dispose() { Clear(); SetVisible(false); assistance.Dispose(); }
    }
}
