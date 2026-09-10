using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class ClientPlanner : IDisposable
    {
        private readonly HammerAssistance assistance = new HammerAssistance();
        private readonly ProjectionControls controls;
        private readonly ChestObservation chests;
        private readonly PlannerWindow view;
        private ZNetScene scene;
        private float nextProgressCheck;
        public ClientConfig Config { get; }
        public BlueprintProjection Projection { get; private set; }
        public BuildMode Mode { get; private set; } = BuildMode.Assisted;
        public MaterialChecklist Materials { get; } = new MaterialChecklist();
        public string[] Files { get; private set; } = Array.Empty<string>();
        public string Status { get; private set; } = "Choose a blueprint or capture nearby buildings.";
        public bool Visible { get; private set; }
        public string BuildStatus => controls.Held ? "Positioning hologram. Release modifiers to build." : assistance.Status;

        public ClientPlanner(ClientConfig config)
        {
            Config = config;
            controls = new ProjectionControls(() => !Visible && Player.m_localPlayer &&
                !Player.m_localPlayer.IsDead() && Player.m_localPlayer.TakeInput() && !Hud.IsPieceSelectionVisible()
                ? Projection : null);
            chests = new ChestObservation(Materials);
            view = new PlannerWindow(this);
        }

        public void Update()
        {
            if (scene != ZNetScene.instance)
            {
                Clear();
                chests.Dispose();
                SetVisible(false);
                scene = ZNetScene.instance;
            }
            if (!Player.m_localPlayer || Player.m_localPlayer.IsDead())
            {
                Mode = BuildMode.Assisted;
                SetVisible(false);
                assistance.SetProjection(null);
                view.Hide();
                return;
            }
            if (Projection != null)
            {
                chests.Update();
                if (Time.time >= nextProgressCheck)
                {
                    ProjectionProgress.Refresh(Projection);
                    nextProgressCheck = Time.time + 0.5f;
                }
            }
            bool otherInput = (Settings.instance && Settings.instance.isActiveAndEnabled) ||
                Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus());
            if (!otherInput)
            {
                if (!Visible && Projection != null && Player.m_localPlayer.TakeInput() && Input.GetKeyDown(Config.AutoBuildKey.Value))
                    SetMode(Mode == BuildMode.Automatic ? BuildMode.Assisted : BuildMode.Automatic);
                if (Input.GetKeyDown(Config.ToggleKey.Value))
                {
                    if (Visible) SetVisible(false);
                    else if (Player.m_localPlayer.TakeInput()) { Refresh(); SetVisible(true); }
                }
                if (Visible && Input.GetKeyDown(KeyCode.Escape)) SetVisible(false);
            }
            UpdateAssistance();
            view.Update();
        }

        public void SetVisible(bool value)
        {
            if (Visible == value) return;
            Visible = value;
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

        public void SetMode(BuildMode mode) { Mode = mode; UpdateAssistance(); }
        private void UpdateAssistance() => assistance.SetProjection(!Visible && !controls.Held && Mode != BuildMode.Guide ? Projection : null, Mode);
        public void DrawProjection()
        {
            if (Player.m_localPlayer && scene == ZNetScene.instance) Projection?.Draw(assistance.SelectedPiece);
        }

        public void Refresh()
        {
            if (BlueprintLibrary.TryList(Config.Directory.Value, out var found, out var error)) Files = found;
            else Status = error;
        }

        public void Capture(string name, float radius)
        {
            if (BlueprintLibrary.TryCapture(Config.Directory.Value, name, Player.m_localPlayer, radius, out var error))
            { Refresh(); Status = $"Saved {name}.blueprint. Your feet define its origin."; }
            else Status = error;
        }

        public bool Load(string file)
        {
            if (!BlueprintLibrary.TryLoad(file, out var document, out var error) ||
                !BlueprintProjection.TryCreate(document, out var next, out error)) { Status = error; return false; }
            Clear();
            Projection = next;
            Projection.Position = Player.m_localPlayer.transform.position;
            ProjectionProgress.Refresh(Projection);
            Status = "Blueprint loaded. Close the planner to position it with modifier keys and the wheel.";
            return true;
        }

        public IReadOnlyList<MaterialChecklist.Row> MaterialRows(bool selectedLayer)
        {
            var costs = new List<KeyValuePair<string, int>>();
            if (Projection != null)
                foreach (var planned in Projection.Pieces.Where(piece => !piece.Completed &&
                    (!selectedLayer || Projection.Layers.Contains(piece.Entry.posY))))
                {
                    var piece = planned.Prefab.GetComponent<Piece>();
                    if (!piece) continue;
                    foreach (var resource in piece.m_resources.Where(resource => resource.m_resItem))
                        costs.Add(new KeyValuePair<string, int>(resource.m_resItem.m_itemData.m_shared.m_name, resource.GetAmount(0)));
                }
            return Materials.Rows(costs, ChestObservation.Items(Player.m_localPlayer.GetInventory()));
        }

        public void ForgetChests() => chests.Dispose();
        public void MoveToFeet()
        {
            if (Projection == null) return;
            Projection.Position = Player.m_localPlayer.transform.position;
            ProjectionProgress.Refresh(Projection);
        }
        public void Clear()
        {
            Mode = BuildMode.Assisted;
            assistance.SetProjection(null);
            Projection?.Dispose();
            Projection = null;
            Materials.ClearChecks();
        }
        public void Dispose() { Clear(); SetVisible(false); chests.Dispose(); view.Dispose(); assistance.Dispose(); controls.Dispose(); }
    }
}
