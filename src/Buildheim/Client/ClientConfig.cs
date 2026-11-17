using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class ClientConfig
    {
        public string DataDirectory { get; }
        public ConfigEntry<string> Directory { get; }
        public ConfigEntry<KeyCode> ToggleKey { get; }
        public ConfigEntry<KeyCode> AutoBuildKey { get; }
        public ConfigEntry<KeyCode> PlacementKey { get; }
        public ConfigEntry<KeyCode> HudKey { get; }
        public ConfigEntry<bool> ShowHud { get; }
        public ConfigEntry<KeyCode> MoveKey { get; }
        public ConfigEntry<KeyCode> MoveAlternateKey { get; }
        public ConfigEntry<KeyCode> HeightKey { get; }
        public ConfigEntry<KeyCode> HeightAlternateKey { get; }
        public ConfigEntry<KeyCode> SidewaysKey { get; }
        public ConfigEntry<KeyCode> LargerStepKey { get; }
        public ConfigEntry<KeyCode> LargerStepAlternateKey { get; }

        // Read on each use so changes from a configuration manager apply immediately.
        public PositioningKeys Positioning => new PositioningKeys(
            new ModifierKey(MoveKey.Value, MoveAlternateKey.Value),
            new ModifierKey(HeightKey.Value, HeightAlternateKey.Value),
            SidewaysKey.Value,
            new ModifierKey(LargerStepKey.Value, LargerStepAlternateKey.Value));

        public ClientConfig(ConfigFile config)
        {
            DataDirectory = ClientPaths.DataDirectory(Paths.ConfigPath);
            Directory = config.Bind("Client", "Blueprint directory",
                Path.Combine(DataDirectory, "blueprints"),
                "Local directory for .blueprint and .vbuild files. Nothing is uploaded to the server.");
            ToggleKey = config.Bind("Client", "Planner key", KeyCode.End, "Open or close the blueprint planner.");
            AutoBuildKey = config.Bind("Client", "Autobuild key", KeyCode.Home, "Toggle autobuild for the current blueprint while the planner is closed.");
            PlacementKey = config.Bind("Client", "Placement key", KeyCode.F7, "Enable or disable the current placement without clearing it. Disabling hides the hologram and stops build assistance.");
            HudKey = config.Bind("Client", "HUD key", KeyCode.F8, "Show or hide Buildheim's HUD details while the planner is closed. The placement-active indicator stays visible. Does not change placement or build mode.");
            ShowHud = config.Bind("Client", "Show HUD", true, "Show Buildheim's build status and shortcut hints. The placement-active indicator stays visible. Hiding the HUD does not stop autobuild or hide the hologram.");

            // Shared by blueprint placement and capture-box corners. Hold Move or Height to position with the wheel.
            const string section = "Positioning";
            var defaults = PositioningKeys.Default;
            MoveKey = config.Bind(section, "Move modifier", defaults.Move.Primary,
                "Hold and use the mouse wheel to move the hologram or capture corner forward or back. Hold with the height modifier to rotate the hologram. While held, hammer building pauses and crouching is ignored.");
            MoveAlternateKey = config.Bind(section, "Move modifier alternate", defaults.Move.Alternate,
                "A second key that works like the move modifier. Set to None to use only one key.");
            HeightKey = config.Bind(section, "Height modifier", defaults.Height.Primary,
                "Hold and use the mouse wheel to raise or lower the hologram or capture corner. While held, hammer building pauses.");
            HeightAlternateKey = config.Bind(section, "Height modifier alternate", defaults.Height.Alternate,
                "A second key that works like the height modifier. Set to None to use only one key.");
            SidewaysKey = config.Bind(section, "Sideways key", defaults.Sideways,
                "Hold with the move modifier to move sideways instead of forward or back. Sitting is ignored while both are held.");
            LargerStepKey = config.Bind(section, "Larger steps modifier", defaults.Larger.Primary,
                "Hold while positioning to use 1 m and 90° steps instead of 0.1 m and 22.5°.");
            LargerStepAlternateKey = config.Bind(section, "Larger steps modifier alternate", defaults.Larger.Alternate,
                "A second key that works like the larger steps modifier. Set to None to use only one key.");
        }
    }
}
