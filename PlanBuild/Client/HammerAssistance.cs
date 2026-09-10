using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class HammerAssistance : IDisposable
    {
        private static HammerAssistance instance;
        private static HammerTarget ghostTarget;
        private readonly Harmony harmony = new Harmony(PlanBuildPlugin.PluginGUID + ".hammer");
        private readonly AutoBuilder autoBuilder = new AutoBuilder();
        private BuildMode mode;
        private BlueprintProjection projection;
        private HammerTarget target;
        public BlueprintProjection.ProjectedPiece SelectedPiece => target?.Planned;
        public string Status { get; private set; } = "Equip a hammer and aim at a missing piece.";
        public bool Ready { get; private set; }

        public HammerAssistance()
        {
            instance = this;
            try
            {
                harmony.PatchAll(typeof(HammerAssistance));
                Ready = true;
            }
            catch (Exception ex)
            {
                harmony.UnpatchSelf();
                Status = "Hammer assistance could not load for this game version. Holograms still work; check the BepInEx log.";
                Jotunn.Logger.LogError($"Cannot patch hammer placement: {ex}");
            }
        }

        public void SetProjection(BlueprintProjection value, BuildMode buildMode = BuildMode.Assisted)
        {
            if (projection == value && mode == buildMode) return;
            projection = value;
            mode = buildMode;
            target = null;
            autoBuilder.Reset();
        }

        private bool Active(Player player) => Ready && projection != null && player == Player.m_localPlayer &&
            !player.IsDead() && player.GetRightItem()?.m_dropPrefab?.name == "Hammer";

        [HarmonyPrefix, HarmonyPatch(typeof(Player), "UpdatePlacement")]
        private static void SelectPiece(Player __instance, bool takeInput, out bool __state)
        {
            __state = false;
            if (instance == null || __instance != Player.m_localPlayer) return;
            instance.target = null;
            if (!instance.Active(__instance) || !takeInput || Hud.IsPieceSelectionVisible()) return;
            if (instance.mode == BuildMode.Automatic && (ZInput.GetButton("Remove") ||
                ZInput.GetButton("JoyRemove") || ZInput.GetButton("JoyAltKeys"))) return;
            var selected = instance.mode == BuildMode.Automatic
                ? instance.autoBuilder.Find(instance.projection, __instance)
                : HammerTarget.Find(instance.projection, __instance);
            instance.target = selected;
            if (selected == null)
            {
                instance.Status = instance.mode == BuildMode.Automatic
                    ? "Waiting for a nearby buildable piece, materials or stamina."
                    : "Aim at a missing piece within hammer reach.";
                return;
            }
            if (!__instance.m_knownRecipes.Contains(selected.Piece.m_name) || !__instance.SetSelectedPiece(selected.Piece))
            {
                instance.Status = "Learn this hammer recipe before building it.";
                return;
            }
            instance.Status = selected.HasInventoryResources()
                ? "Click to build " + Localization.instance.Localize(selected.Piece.m_name)
                : "Missing materials in your inventory for " + Localization.instance.Localize(selected.Piece.m_name);
            if (instance.mode != BuildMode.Automatic) return;
            // Validate first, then queue one ordinary hammer click. UpdatePlacement remains
            // responsible for placement, materials, stamina, durability and the tool cooldown.
            __instance.UpdatePlacementGhost(false);
            if (__instance.m_placementStatus != Player.PlacementStatus.Valid)
            {
                instance.Status = "Placement blocked. Trying other nearby blueprint pieces.";
                return;
            }
            __instance.m_placePressedTime = Time.time;
            __state = true;
            instance.Status = "Autobuilding " + Localization.instance.Localize(selected.Piece.m_name);
        }

        [HarmonyFinalizer, HarmonyPatch(typeof(Player), "UpdatePlacement")]
        private static void ClearAutoClick(Player __instance, bool __state)
        {
            // An early return (for example, opening the hammer menu) must not leave a queued click.
            if (__state) __instance.m_placePressedTime = -9999f;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static bool CheckBuild(Player __instance, Piece piece, ref bool __result)
        {
            if (instance == null || !instance.Active(__instance)) return true;
            var selected = instance.target;
            ProjectionProgress.Refresh(instance.projection);
            string error = null;
            if (selected == null || selected.Piece != piece || selected.Planned.Completed)
                error = "Aim at a missing blueprint piece to build it.";
            else if (!__instance.m_knownRecipes.Contains(piece.m_name) || !__instance.IsPieceAvailable(piece))
                error = "Learn this hammer recipe before building it.";
            else if (ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
                error = "Hammer assistance requires resource costs. Disable the world's free-build setting first.";
            else if (!selected.HasInventoryResources() || !__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                error = "Missing materials or a required crafting station. Carry the materials and build within station range.";
            if (error == null) return true;
            __result = false;
            __instance.Message(MessageHud.MessageType.Center, error);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static void Built(Player __instance, bool __result)
        {
            if (__result && instance != null && instance.Active(__instance) && instance.target != null)
                instance.target.Planned.Completed = true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        private static void BeginGhost(Player __instance, out HammerTarget __state)
        {
            __state = ghostTarget;
            ghostTarget = null;
            if (instance == null || !instance.Active(__instance)) return;
            var selected = instance.target;
            if (selected == null || selected.Planned.Completed || __instance.GetSelectedPiece() != selected.Piece ||
                !__instance.m_placementGhost) return;
            ghostTarget = selected;
            __instance.m_placementGhost.transform.SetPositionAndRotation(selected.Position, selected.Rotation);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        private static void EndGhost(Player __instance)
        {
            if (instance == null || !instance.Active(__instance)) return;
            if (ghostTarget == null)
            {
                __instance.m_placementStatus = Player.PlacementStatus.Invalid;
                if (__instance.m_placementGhost) __instance.m_placementGhost.SetActive(false);
            }
        }

        [HarmonyFinalizer, HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        private static void ClearGhost(HammerTarget __state) => ghostTarget = __state;

        [HarmonyPrefix, HarmonyPatch(typeof(Player), "PieceRayTest")]
        private static bool AutoSurface(Player __instance, bool water, ref Vector3 point, ref Vector3 normal,
            ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface, ref bool __result)
        {
            if (ghostTarget == null || ghostTarget.Player != __instance || instance.mode != BuildMode.Automatic) return true;
            point = Vector3.zero;
            normal = Vector3.zero;
            piece = null;
            heightmap = null;
            waterSurface = null;
            __result = ghostTarget.TryAutoSurface(water, out var hit);
            if (__result)
            {
                point = hit.point;
                normal = hit.normal;
                piece = hit.collider.GetComponentInParent<Piece>();
                heightmap = hit.collider.GetComponent<Heightmap>();
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water")) waterSurface = hit.collider;
            }
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "PieceRayTest")]
        private static void CheckSurface(Player __instance, ref Vector3 point, ref bool __result)
        {
            if (ghostTarget == null || ghostTarget.Player != __instance || !__result) return;
            // Preserve vanilla's real surface normal, terrain, water and support piece. Only move
            // the placement point when the hit surface actually touches the projected geometry.
            if (!ghostTarget.NearSurface(point) || Vector3.Distance(__instance.m_eye.position, ghostTarget.Position) >=
                __instance.m_maxPlaceDistance + ghostTarget.Piece.m_extraPlacementDistance)
            {
                __result = false;
                instance.Status = "Aim at the hologram near an existing surface, within hammer reach.";
                return;
            }
            point = ghostTarget.Position;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        private static IEnumerable<CodeInstruction> AlignGhost(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var position = AccessTools.PropertySetter(typeof(Transform), nameof(Transform.position));
            var rotation = AccessTools.PropertySetter(typeof(Transform), nameof(Transform.rotation));
            int positions = 0, rotations = 0;
            foreach (var instruction in code)
            {
                if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(Transform) &&
                    (method.Name == "SetPositionAndRotation" || method.Name == "SetLocalPositionAndRotation" ||
                     method.Name == "set_localPosition" || method.Name == "set_localRotation"))
                    throw new InvalidOperationException("UpdatePlacementGhost uses unsupported transform writes; assistance was disabled.");
                if (instruction.Calls(position))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(HammerAssistance), nameof(SetPosition));
                    positions++;
                }
                else if (instruction.Calls(rotation))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(HammerAssistance), nameof(SetRotation));
                    rotations++;
                }
            }
            if (positions == 0 || rotations == 0)
                throw new InvalidOperationException("UpdatePlacementGhost no longer sets position and rotation as expected; assistance was disabled.");
            return code;
        }

        // Keep the ghost aligned throughout the ORIGINAL validation routine, including checks
        // that happen before final snapping. Marker transforms and ordinary building are untouched.
        private static void SetPosition(Transform transform, Vector3 position)
        {
            transform.position = ghostTarget != null && transform == ghostTarget.Player.m_placementGhost.transform
                ? ghostTarget.Position : position;
        }

        private static void SetRotation(Transform transform, Quaternion rotation)
        {
            transform.rotation = ghostTarget != null && transform == ghostTarget.Player.m_placementGhost.transform
                ? ghostTarget.Rotation : rotation;
        }

        public void Dispose()
        {
            harmony.UnpatchSelf();
            projection = null;
            target = null;
            ghostTarget = null;
            instance = null;
        }
    }
}
