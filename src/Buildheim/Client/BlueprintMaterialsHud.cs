using System;
using HarmonyLib;

namespace PlanBuild.Client
{
    // Borrow the native material slots while Repair is selected. Rendering a
    // blueprint's costs must never change the player's actual hammer selection.
    internal sealed class BlueprintMaterialsHud : IDisposable
    {
        private static BlueprintMaterialsHud instance;
        private readonly Harmony harmony = new Harmony(PlanBuildPlugin.PluginGUID + ".materialHud");
        private readonly Func<Piece> hoveredPiece;

        public BlueprintMaterialsHud(Func<Piece> hoveredPiece)
        {
            this.hoveredPiece = hoveredPiece;
            instance = this;
            harmony.PatchAll(typeof(BlueprintMaterialsHud));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
        private static void ShowBlueprintMaterials(Hud __instance, Piece piece)
        {
            if (instance == null || !piece || !piece.m_repairPiece) return;
            var hovered = instance.hoveredPiece();
            if (!hovered || hovered.m_repairPiece || hovered.m_removePiece) return;

            string title = __instance.m_buildSelection.text;
            var icon = __instance.m_buildIcon.sprite;
            bool iconEnabled = __instance.m_buildIcon.enabled;
            bool snapping = __instance.m_snappingIcon.enabled;
            var snappingIcon = __instance.m_snappingIcon.sprite;
            // This reuses the game's resource icons, shortage colors, counts and
            // station indicator. The nested call has a non-Repair piece, so the
            // guard above prevents recursion. Vanilla clears the slots again
            // when hovering ends, the planner opens or Buildheim's HUD is hidden.
            try
            {
                __instance.SetupPieceInfo(hovered);
                __instance.m_pieceDescription.text = "Blueprint materials: " + Localization.instance.Localize(hovered.m_name);
            }
            finally
            {
                __instance.m_buildSelection.text = title;
                __instance.m_buildIcon.sprite = icon;
                __instance.m_buildIcon.enabled = iconEnabled;
                __instance.m_snappingIcon.enabled = snapping;
                __instance.m_snappingIcon.sprite = snappingIcon;
            }
        }

        public void Dispose()
        {
            harmony.UnpatchSelf();
            instance = null;
        }
    }
}
