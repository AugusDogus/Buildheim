using System;
using HarmonyLib;
using UnityEngine;

namespace PlanBuild.Client
{
    // Consume the same wheel sample for both hammer rotation and camera zoom, regardless of Update order.
    internal sealed class ProjectionControls : IDisposable
    {
        private static ProjectionControls instance;
        private readonly Harmony harmony = new Harmony(PlanBuildPlugin.PluginGUID + ".positioning");
        private readonly Func<BlueprintProjection> activeProjection;
        private readonly Func<PositioningKeys> keys;
        private int lastFrame = -1;
        public static bool Adjusting => instance != null && instance.Held;
        public bool Held => activeProjection() != null && (Move || Height);
        private bool Move => keys().Move.Held(Input.GetKey);
        private bool Height => keys().Height.Held(Input.GetKey);

        public ProjectionControls(Func<BlueprintProjection> activeProjection, Func<PositioningKeys> keys)
        {
            this.activeProjection = activeProjection;
            this.keys = keys;
            instance = this;
            harmony.PatchAll(typeof(ProjectionControls));
        }

        // Read even when vanilla skips camera zoom and hammer rotation. The postfix below
        // handles this sample once; all later consumers in the frame receive zero.
        public void Update() { if (Held) ZInput.GetMouseScrollWheel(); }

        [HarmonyPostfix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
        private static void Scroll(ref float __result)
        {
            if (instance == null || !instance.Held) return;
            float wheel = __result;
            __result = 0;
            if (wheel == 0 || instance.lastFrame == Time.frameCount) return;
            instance.lastFrame = Time.frameCount;
            var projection = instance.activeProjection();
            var keys = instance.keys();
            float direction = Mathf.Sign(wheel);
            bool coarse = keys.Larger.Held(Input.GetKey);
            bool move = instance.Move, height = instance.Height;
            if (move && height) projection.Yaw = BlueprintRotation.Turn(projection.Yaw, (int)direction, coarse);
            else
            {
                var axis = Vector3.up;
                if (!height && GameCamera.instance)
                {
                    var forward = Quaternion.Euler(0, GameCamera.instance.transform.eulerAngles.y, 0);
                    axis = forward * (keys.SidewaysHeld(Input.GetKey) ? Vector3.right : Vector3.forward);
                }
                projection.Position += axis * direction * (coarse ? 1f : 0.1f);
            }
            ProjectionProgress.Refresh(projection);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static bool PreventModifierActions(string name, ref bool __result)
        {
            if (instance == null || !instance.Held || !instance.Move) return true;
            if (name != "Crouch" && !(name == "Sit" && instance.keys().SidewaysHeld(Input.GetKey))) return true;
            __result = false;
            return false;
        }

        public void Dispose() { harmony.UnpatchSelf(); instance = null; }
    }
}
