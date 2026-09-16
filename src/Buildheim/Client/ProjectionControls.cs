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
        private readonly RotationInputReport inputReport;
        private int lastFrame = -1;
        public const string Hints = "Ctrl + wheel: forward/back   |   Ctrl + X + wheel: sideways\nAlt + wheel: height   |   Ctrl + Alt + wheel: rotate   |   Shift: larger steps";
        public static bool Adjusting => instance != null && instance.Held;
        public bool Held => activeProjection() != null && (Control || Alt);
        // Read modifiers from the same input system as ZInput.GetMouseScrollWheel.
        private static bool Control => ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);
        private static bool Alt => ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);

        public ProjectionControls(Func<BlueprintProjection> activeProjection, Func<bool> logRotationInput)
        {
            this.activeProjection = activeProjection;
            inputReport = new RotationInputReport(logRotationInput);
            instance = this;
            harmony.PatchAll(typeof(ProjectionControls));
        }

        // Read even when vanilla skips camera zoom and hammer rotation. The postfix below
        // handles this sample once; all later consumers in the frame receive zero.
        public void Update()
        {
            inputReport.Observe(activeProjection() != null);
            if (Held) ZInput.GetMouseScrollWheel();
        }

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
            bool leftShift = ZInput.GetKey(KeyCode.LeftShift);
            bool rightShift = ZInput.GetKey(KeyCode.RightShift);
            bool coarse = leftShift || rightShift;
            if (Control && Alt)
            {
                float before = projection.Yaw;
                projection.Yaw = BlueprintRotation.Turn(before, (int)direction, coarse);
                instance.inputReport.Rotation(wheel, coarse, before, projection.Yaw);
            }
            else
            {
                var axis = Vector3.up;
                if (!Alt && GameCamera.instance)
                {
                    var forward = Quaternion.Euler(0, GameCamera.instance.transform.eulerAngles.y, 0);
                    axis = forward * (ZInput.GetKey(KeyCode.X) ? Vector3.right : Vector3.forward);
                }
                projection.Position += axis * direction * (coarse ? 1f : 0.1f);
            }
            ProjectionProgress.Refresh(projection);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static bool PreventModifierActions(string name, ref bool __result)
        {
            if (instance == null || !instance.Held || !Control) return true;
            if (name != "Crouch" && !(name == "Sit" && ZInput.GetKey(KeyCode.X))) return true;
            __result = false;
            return false;
        }

        public void Dispose() { inputReport.Dispose(); harmony.UnpatchSelf(); instance = null; }
    }
}
