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
        private readonly Func<bool> logRotationInput;
        private int lastFrame = -1;
        private int loggedRotations;
        public const string Hints = "Ctrl + wheel: forward/back   |   Ctrl + X + wheel: sideways\nAlt + wheel: height   |   Ctrl + Alt + wheel: rotate   |   Shift: larger steps";
        public static bool Adjusting => instance != null && instance.Held;
        public bool Held => activeProjection() != null && (Control || Alt);
        // Read modifiers from the same input system as ZInput.GetMouseScrollWheel.
        private static bool Control => ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl);
        private static bool Alt => ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetKey(KeyCode.RightAlt);

        public ProjectionControls(Func<BlueprintProjection> activeProjection, Func<bool> logRotationInput)
        {
            this.activeProjection = activeProjection;
            this.logRotationInput = logRotationInput;
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
            bool leftShift = ZInput.GetKey(KeyCode.LeftShift);
            bool rightShift = ZInput.GetKey(KeyCode.RightShift);
            bool coarse = leftShift || rightShift;
            if (Control && Alt)
            {
                float before = projection.Yaw;
                projection.Yaw = BlueprintRotation.Turn(before, (int)direction, coarse);
                instance.TraceRotation(wheel, leftShift, rightShift, before, projection.Yaw);
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

        // Read legacy state only for comparison. It must not affect positioning.
        private void TraceRotation(float wheel, bool leftShift, bool rightShift, float before, float after)
        {
            if (!logRotationInput() || loggedRotations >= 100) return;
            if (loggedRotations == 0)
                Jotunn.Logger.LogInfo($"Buildheim rotation trace: build={typeof(ProjectionControls).Assembly.ManifestModule.ModuleVersionId}; logging up to 100 rotation steps.");
            loggedRotations++;
            Jotunn.Logger.LogInfo(FormattableString.Invariant(
                $"Buildheim rotation trace: sample={loggedRotations} frame={Time.frameCount} time={Time.realtimeSinceStartup:F3} wheel={wheel:R} yaw={before:R}->{after:R} delta={Mathf.DeltaAngle(before, after):R} ") +
                $"focused={Application.isFocused} " +
                $"shiftL={leftShift} shiftR={rightShift} " +
                $"legacyShiftL={Input.GetKey(KeyCode.LeftShift)} legacyShiftR={Input.GetKey(KeyCode.RightShift)}");
            if (loggedRotations == 100)
                Jotunn.Logger.LogInfo("Buildheim rotation trace: limit reached; restart the game to record another reproduction.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static bool PreventModifierActions(string name, ref bool __result)
        {
            if (instance == null || !instance.Held || !Control) return true;
            if (name != "Crouch" && !(name == "Sit" && ZInput.GetKey(KeyCode.X))) return true;
            __result = false;
            return false;
        }

        public void Dispose() { harmony.UnpatchSelf(); instance = null; }
    }
}
