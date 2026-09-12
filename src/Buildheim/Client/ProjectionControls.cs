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
        private int lastFrame = -1;
        public const string Hints = "Ctrl + wheel: forward/back   |   Ctrl + X + wheel: sideways\nAlt + wheel: height   |   Ctrl + Alt + wheel: rotate   |   Shift: larger steps";
        public static bool Adjusting => instance != null && instance.Held;
        public bool Held => activeProjection() != null && (Control || Alt);
        private static bool Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        public ProjectionControls(Func<BlueprintProjection> activeProjection)
        {
            this.activeProjection = activeProjection;
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
            float direction = Mathf.Sign(wheel);
            bool coarse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Control && Alt) projection.Yaw = BlueprintRotation.Turn(projection.Yaw, (int)direction, coarse);
            else
            {
                var axis = Vector3.up;
                if (!Alt && GameCamera.instance)
                {
                    var forward = Quaternion.Euler(0, GameCamera.instance.transform.eulerAngles.y, 0);
                    axis = forward * (Input.GetKey(KeyCode.X) ? Vector3.right : Vector3.forward);
                }
                projection.Position += axis * direction * (coarse ? 1f : 0.1f);
            }
            ProjectionProgress.Refresh(projection);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static bool PreventModifierActions(string name, ref bool __result)
        {
            if (instance == null || !instance.Held || !Control) return true;
            if (name != "Crouch" && !(name == "Sit" && Input.GetKey(KeyCode.X))) return true;
            __result = false;
            return false;
        }

        public void Dispose() { harmony.UnpatchSelf(); instance = null; }
    }
}
