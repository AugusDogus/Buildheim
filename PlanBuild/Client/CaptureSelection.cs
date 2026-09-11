using System;
using HarmonyLib;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class CaptureSelection : IDisposable
    {
        private static CaptureSelection instance;
        private readonly Harmony harmony = new Harmony(PlanBuildPlugin.PluginGUID + ".capture");
        private readonly Func<bool> canTakeInput;
        private readonly CaptureOutline outline = new CaptureOutline();
        private int startedFrame, scrollFrame = -1;
        private bool firstSelected = true;
        public Vector3? First { get; private set; }
        public Vector3? Second { get; private set; }
        public bool Editing { get; private set; }
        public string SelectedCorner => firstSelected ? "A" : "B";
        public string Status { get; private set; } = "Aim at the ground or a building and pick two opposite corners.";
        public const string Hints = "Left click: corner A | Right click: corner B | Middle click: switch corner\nCtrl + wheel: forward/back | Ctrl + X + wheel: sideways | Alt + wheel: height | Shift: 1 m";
        private bool Active => Editing && canTakeInput();
        private static bool Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        public CaptureSelection(Func<bool> canTakeInput)
        {
            this.canTakeInput = canTakeInput;
            instance = this;
            harmony.PatchAll(typeof(CaptureSelection));
        }

        public void Begin() { Editing = true; startedFrame = Time.frameCount; }
        public void End() => Editing = false;
        public void Clear() { End(); First = Second = null; }

        public bool TryBounds(out CaptureBounds bounds)
        {
            bounds = null;
            return First.HasValue && Second.HasValue && CaptureBounds.TryCreate(
                First.Value.x, First.Value.y, First.Value.z, Second.Value.x, Second.Value.y, Second.Value.z, out bounds);
        }

        public void Update()
        {
            if (!Active || Time.frameCount == startedFrame) return;
            if (Input.GetMouseButtonDown(2)) firstSelected = !firstSelected;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                firstSelected = Input.GetMouseButtonDown(0);
                if (TryAim(out var point))
                {
                    if (firstSelected) First = point; else Second = point;
                    Status = $"Corner {SelectedCorner} set. Use modifier keys and the wheel to adjust it.";
                }
                else Status = "No surface within 100 m. Aim at the ground or a building to set a corner.";
            }
            if (Control || Alt) ZInput.GetMouseScrollWheel();
        }

        private static bool TryAim(out Vector3 point)
        {
            point = Vector3.zero;
            if (!GameCamera.instance) return false;
            var camera = GameCamera.instance.GetComponent<Camera>();
            int layers = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
            if (!Physics.Raycast(camera.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out var hit, 100, layers, QueryTriggerInteraction.Ignore)) return false;
            point = hit.point;
            return true;
        }

        public void Draw(bool showInMenu)
        {
            if (!Editing && !showInMenu) return;
            Vector3? second = Second;
            if (Active && First.HasValue && !second.HasValue && TryAim(out var point)) second = point;
            outline.Draw(First, second);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
        private static void Scroll(ref float __result)
        {
            if (instance == null || !instance.Active || (!Control && !Alt)) return;
            float wheel = __result;
            __result = 0;
            if (wheel == 0 || instance.scrollFrame == Time.frameCount) return;
            instance.scrollFrame = Time.frameCount;
            Vector3? selected = instance.firstSelected ? instance.First : instance.Second;
            if (!selected.HasValue) return;
            var axis = Vector3.up;
            if (!Alt && GameCamera.instance)
            {
                var rotation = Quaternion.Euler(0, GameCamera.instance.transform.eulerAngles.y, 0);
                axis = rotation * (Input.GetKey(KeyCode.X) ? Vector3.right : Vector3.forward);
            }
            bool coarse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var position = selected.Value + axis * Mathf.Sign(wheel) * (coarse ? 1f : 0.1f);
            if (instance.firstSelected) instance.First = position; else instance.Second = position;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        private static void SuppressActions(Player __instance, ref bool attack, ref bool attackHold,
            ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool crouch, ref bool dodge)
        {
            if (instance == null || !instance.Active || __instance != Player.m_localPlayer) return;
            attack = attackHold = secondaryAttack = secondaryAttackHold = block = blockHold = dodge = false;
            if (Control) crouch = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Player), "UpdatePlacement")]
        private static bool SuppressPlacement(Player __instance)
        {
            if (instance == null || !instance.Editing || __instance != Player.m_localPlayer) return true;
            __instance.m_placePressedTime = __instance.m_removePressedTime = -9999f;
            if (__instance.m_placementGhost) __instance.m_placementGhost.SetActive(false);
            if (__instance.m_placementMarkerInstance) __instance.m_placementMarkerInstance.SetActive(false);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static bool SuppressSit(string name, ref bool __result)
        {
            if (instance == null || !instance.Active || !Control || name != "Sit" || !Input.GetKey(KeyCode.X)) return true;
            __result = false;
            return false;
        }

        public void Dispose() { harmony.UnpatchSelf(); instance = null; outline.Dispose(); }
    }
}
