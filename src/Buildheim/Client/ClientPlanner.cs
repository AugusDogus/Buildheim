using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly PlacementSession session = new PlacementSession();
        private ZNetScene scene;
        private float nextProgressCheck;
        public ClientConfig Config { get; }
        public BlueprintProjection Projection { get; private set; }
        public CaptureSelection Selection { get; }
        public bool PlacementEnabled => Projection != null && Projection.Enabled;
        private BlueprintProjection ActiveProjection => PlacementEnabled && !Selection.Editing ? Projection : null;
        public BuildMode Mode { get; private set; } = BuildMode.Assisted;
        public MaterialChecklist Materials { get; } = new MaterialChecklist();
        public string[] Files { get; private set; } = Array.Empty<string>();
        public string Status { get; private set; } = "Choose a blueprint or select a box to capture a building.";
        public bool Visible { get; private set; }
        public string BuildStatus => controls.Held ? "Positioning hologram. Release modifiers to build." : assistance.Status;

        public ClientPlanner(ClientConfig config)
        {
            Config = config;
            Selection = new CaptureSelection(() => !Visible && Player.m_localPlayer &&
                !Player.m_localPlayer.IsDead() && Player.m_localPlayer.TakeInput() && !Hud.IsPieceSelectionVisible());
            controls = new ProjectionControls(() => !Visible && Player.m_localPlayer &&
                !Player.m_localPlayer.IsDead() && Player.m_localPlayer.TakeInput() && !Hud.IsPieceSelectionVisible()
                ? ActiveProjection : null);
            chests = new ChestObservation(Materials);
            view = new PlannerWindow(this);
        }

        public void Update()
        {
            if (scene != ZNetScene.instance)
            {
                SaveSession();
                ResetProjection();
                session.Reset();
                chests.Dispose();
                Selection.Clear();
                SetVisible(false, false);
                view.Hide();
                scene = ZNetScene.instance;
            }
            if (!Player.m_localPlayer || Player.m_localPlayer.IsDead())
            {
                SaveSession();
                if (Mode == BuildMode.Automatic) Mode = BuildMode.Assisted;
                Selection.End();
                SetVisible(false, false);
                assistance.SetProjection(null);
                view.Hide();
                return;
            }
            if (!session.Active && ZNet.instance && ZNet.instance.GetWorld() != null && Player.m_localPlayer.GetPlayerID() != 0) RestoreSession();
            if (Projection != null)
            {
                chests.Update();
                if (Time.time >= nextProgressCheck)
                {
                    if (PlacementEnabled) ProjectionProgress.Refresh(Projection);
                    nextProgressCheck = Time.time + 0.5f;
                    SaveSession();
                }
            }
            bool otherInput = (Settings.instance && Settings.instance.isActiveAndEnabled) ||
                Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus());
            if (!otherInput)
            {
                if (!Visible && Player.m_localPlayer.TakeInput())
                {
                    if (Input.GetKeyDown(Config.PlacementKey.Value)) SetPlacementEnabled(!PlacementEnabled);
                    if (Input.GetKeyDown(Config.HudKey.Value)) ToggleHud();
                    if (!Selection.Editing && PlacementEnabled && Input.GetKeyDown(Config.AutoBuildKey.Value))
                        SetMode(Mode == BuildMode.Automatic ? BuildMode.Assisted : BuildMode.Automatic);
                }
                if (Input.GetKeyDown(Config.ToggleKey.Value))
                {
                    if (Visible) SetVisible(false);
                    else if (Player.m_localPlayer.TakeInput()) { Refresh(); SetVisible(true); }
                }
                if (Visible && Input.GetKeyDown(KeyCode.Escape)) SetVisible(false);
            }
            controls.Update();
            Selection.Update();
            UpdateAssistance();
            view.Update();
        }

        public void SetVisible(bool value, bool playSound = true)
        {
            if (Visible == value) return;
            Visible = value;
            if (value) Hud.HidePieceSelection();
            if (playSound) PlannerPresentation.PlaySound(value);
            if (value && Selection.Editing) { Selection.End(); view.SelectCapture(); }
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

        public void SetPlacementEnabled(bool enabled)
        {
            if (Projection == null || Projection.Enabled == enabled) return;
            Projection.Enabled = enabled;
            if (!enabled && Mode == BuildMode.Automatic) Mode = BuildMode.Assisted;
            if (enabled) ProjectionProgress.Refresh(Projection);
            UpdateAssistance();
            Status = enabled ? "Placement enabled. Autobuild is off." :
                "Placement disabled. Its position, layers and checklist are preserved.";
            SaveSession();
        }

        public void SetMode(BuildMode mode)
        {
            if (!PlacementEnabled) return;
            Mode = mode;
            UpdateAssistance();
        }
        public void ToggleHud() => Config.ShowHud.Value = !Config.ShowHud.Value;
        private void UpdateAssistance() => assistance.SetProjection(!Visible && !controls.Held && Mode != BuildMode.Guide ? ActiveProjection : null, Mode);
        public void DrawProjection()
        {
            if (Player.m_localPlayer && !Player.m_localPlayer.IsDead() && scene == ZNetScene.instance)
            {
                ActiveProjection?.Draw(assistance.SelectedPiece, assistance.SelectedBuildable);
                Selection.Draw(Visible && view.CaptureVisible);
            }
        }

        public void Refresh()
        {
            if (BlueprintLibrary.TryList(Config.Directory.Value, out var found, out var error)) Files = found;
            else Status = error;
        }

        public void OpenBlueprintFolder()
        {
            try
            {
                string directory = Path.GetFullPath(Config.Directory.Value);
                Directory.CreateDirectory(directory);
                using (System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(directory)
                {
                    UseShellExecute = true,
                    Verb = "open"
                })) { }
                Status = "Folder open requested. Add .blueprint or .vbuild files, then click Refresh.";
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is ArgumentException || ex is NotSupportedException || ex is System.ComponentModel.Win32Exception ||
                ex is System.Security.SecurityException || ex is InvalidOperationException)
            {
                Status = "Cannot open the blueprint folder. Check the game log for its path and open it manually.";
                Jotunn.Logger.LogWarning($"Cannot open blueprint folder '{Config.Directory.Value}': {ex.Message} " +
                    "Check the directory setting, permissions, and default file manager.");
            }
        }

        public void BeginCapture()
        {
            if (Mode == BuildMode.Automatic) Mode = BuildMode.Assisted;
            Selection.Begin();
            SetVisible(false);
            Status = $"Select corners A and B, then press {Config.ToggleKey.Value} to review and save.";
        }

        public void Capture(string name)
        {
            if (!Selection.TryBounds(out var bounds) || !Selection.First.HasValue)
            { Status = "Select both corners before saving a blueprint."; return; }
            if (BlueprintLibrary.TryCapture(Config.Directory.Value, name, bounds, Selection.First.Value, out var error))
            { Refresh(); Selection.End(); Status = $"Saved {name}.blueprint. Corner A defines its origin."; }
            else Status = error;
        }

        public bool Load(string file)
        {
            if (!BlueprintLibrary.TryLoad(file, out var document, out var error) ||
                !BlueprintProjection.TryCreate(document, out var next, out error)) { Status = error; return false; }
            ResetProjection();
            session.SetDocument(document);
            Projection = next;
            Projection.Position = Player.m_localPlayer.transform.position;
            ProjectionProgress.Refresh(Projection);
            Status = "Blueprint loaded. Close the planner to position it with modifier keys and the wheel.";
            SaveSession();
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
        private void RestoreSession()
        {
            string directory = Path.Combine(Config.DataDirectory, "placements");
            if (!session.Open(directory, ZNet.instance.GetWorldUID(), Player.m_localPlayer.GetPlayerID(), out var save, out var error))
            { if (error.Length > 0) Status = error; return; }
            if (!BlueprintDocument.TryParse(save.Name, save.Blueprint, false, out var document, out error) ||
                !BlueprintProjection.TryCreate(document, out var restored, out error))
            { Status = "Saved hologram could not be restored. " + error; return; }
            Projection = restored;
            session.SetDocument(document);
            Projection.Position = new Vector3(save.X, save.Y, save.Z);
            Projection.Yaw = save.Yaw;
            Projection.Enabled = !save.Disabled;
            Projection.Layers.SetHeight(save.LayerHeight);
            bool restoredLayer = Projection.Layers.Select(save.Layer);
            Mode = save.PreviewOnly ? BuildMode.Guide : BuildMode.Assisted;
            foreach (string material in save.CheckedMaterials) Materials.Check(material, true);
            ProjectionProgress.Refresh(Projection);
            Status = restoredLayer ? "Saved hologram restored. Autobuild is off." : "Saved hologram restored with all layers; its saved layer was invalid. Autobuild is off.";
            if (!PlacementEnabled) Status += $" Placement is disabled. Press {Config.PlacementKey.Value} or enable it in Build.";
        }

        private void SaveSession()
        {
            if (session.Save(Projection, Mode, Materials, out var error)) return;
            if (Status != error) Jotunn.Logger.LogError(error);
            Status = error;
        }

        public void Clear()
        {
            ResetProjection();
            Status = session.Clear(out var error) ? "Hologram cleared, including its saved placement." : error;
        }

        private void ResetProjection()
        {
            Mode = BuildMode.Assisted;
            assistance.SetProjection(null);
            Projection?.Dispose();
            Projection = null;
            Materials.ClearChecks();
        }
        public void Dispose() { SaveSession(); ResetProjection(); SetVisible(false, false); chests.Dispose(); view.Dispose(); assistance.Dispose(); controls.Dispose(); Selection.Dispose(); }
    }
}
