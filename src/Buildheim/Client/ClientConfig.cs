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

        public ClientConfig(ConfigFile config)
        {
            DataDirectory = ClientPaths.DataDirectory(Paths.ConfigPath);
            Directory = config.Bind("Client", "Blueprint directory",
                Path.Combine(DataDirectory, "blueprints"),
                "Local directory for .blueprint and .vbuild files. Nothing is uploaded to the server.");
            ToggleKey = config.Bind("Client", "Planner key", KeyCode.End, "Open or close the blueprint planner.");
            AutoBuildKey = config.Bind("Client", "Autobuild key", KeyCode.Home, "Toggle autobuild for the current blueprint while the planner is closed.");
            PlacementKey = config.Bind("Client", "Placement key", KeyCode.F7, "Enable or disable the current placement without clearing it. Disabling hides the hologram and stops build assistance.");
            HudKey = config.Bind("Client", "HUD key", KeyCode.F8, "Show or hide Buildheim's HUD while the planner is closed. Does not change placement or build mode.");
            ShowHud = config.Bind("Client", "Show HUD", true, "Show Buildheim's build status and shortcut hints. Hiding the HUD does not stop autobuild or hide the hologram.");
        }
    }
}
