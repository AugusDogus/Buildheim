using System;
using HarmonyLib;

namespace PlanBuild.Client
{
    // Show the hovered blueprint in the native info card even when its build
    // is blocked. Rendering this card never changes the hammer's selection.
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

            // The nested call has a non-Repair piece, so the guard above prevents
            // recursion. Vanilla restores its own card when hovering ends, the
            // planner opens or Buildheim's HUD is hidden.
            __instance.SetupPieceInfo(hovered);
        }

        public void Dispose()
        {
            harmony.UnpatchSelf();
            instance = null;
        }
    }
}
