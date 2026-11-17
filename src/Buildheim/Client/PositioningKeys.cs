using System;
using UnityEngine;

namespace PlanBuild.Client
{
    // A modifier bound to up to two keys, so the defaults accept both left and right variants.
    // KeyCode.None leaves a slot unbound.
    internal readonly struct ModifierKey
    {
        public KeyCode Primary { get; }
        public KeyCode Alternate { get; }

        public ModifierKey(KeyCode primary, KeyCode alternate)
        {
            Primary = primary;
            Alternate = alternate;
        }

        // Takes the key reader so this type stays independent of Unity's input module.
        public bool Held(Func<KeyCode, bool> isHeld) =>
            (Primary != KeyCode.None && isHeld(Primary)) || (Alternate != KeyCode.None && isHeld(Alternate));

        // A left/right pair shows its shared name ("Ctrl"); other keys use their config-file names.
        public string Label
        {
            get
            {
                if (Primary == KeyCode.None || Primary == Alternate) return PositioningKeys.Name(Alternate);
                if (Alternate == KeyCode.None) return PositioningKeys.Name(Primary);
                string pair = PairName(Primary, Alternate) ?? PairName(Alternate, Primary);
                return pair ?? $"{Primary}/{Alternate}";
            }
        }

        private static string PairName(KeyCode left, KeyCode right)
        {
            string name = left.ToString();
            if (!name.StartsWith("Left", StringComparison.Ordinal) || name.Length == 4) return null;
            string shared = name.Substring(4);
            if (right.ToString() != "Right" + shared) return null;
            return shared == "Control" ? "Ctrl" : shared;
        }
    }

    // The keys that position a blueprint hologram or capture-box corner with the mouse wheel.
    internal readonly struct PositioningKeys
    {
        public static readonly PositioningKeys Default = new PositioningKeys(
            new ModifierKey(KeyCode.LeftControl, KeyCode.RightControl),
            new ModifierKey(KeyCode.LeftAlt, KeyCode.RightAlt),
            KeyCode.X,
            new ModifierKey(KeyCode.LeftShift, KeyCode.RightShift));

        // Moves forward/back; with Height, rotates instead.
        public ModifierKey Move { get; }
        public ModifierKey Height { get; }
        // Held with Move to move sideways instead of forward/back.
        public KeyCode Sideways { get; }
        public ModifierKey Larger { get; }

        public PositioningKeys(ModifierKey move, ModifierKey height, KeyCode sideways, ModifierKey larger)
        {
            Move = move;
            Height = height;
            Sideways = sideways;
            Larger = larger;
        }

        public bool SidewaysHeld(Func<KeyCode, bool> isHeld) => Sideways != KeyCode.None && isHeld(Sideways);

        public string ProjectionHints =>
            $"{Move.Label} + wheel: forward/back   |   {Move.Label} + {Name(Sideways)} + wheel: sideways\n" +
            $"{Height.Label} + wheel: height   |   {Move.Label} + {Height.Label} + wheel: rotate   |   {Larger.Label}: larger steps";

        public string CaptureHints =>
            "Left click: corner A | Right click: corner B | Middle click: switch corner\n" +
            $"{Move.Label} + wheel: forward/back | {Move.Label} + {Name(Sideways)} + wheel: sideways | " +
            $"{Height.Label} + wheel: height | {Larger.Label}: 1 m";

        internal static string Name(KeyCode key) => key == KeyCode.None ? "Unbound" : key.ToString();
    }
}
