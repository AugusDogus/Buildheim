using BepInEx;
using Jotunn.Utils;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.30.0")]
    [BepInIncompatibility("marcopogo.PlanBuild")]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "augusdogus.Buildheim";
        public const string PluginName = "Buildheim";
        public const string PluginVersion = "1.2.1";

        private ClientPlanner planner;

        private void Awake()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            if (!ClientPaths.TryMigrateConfig(BepInEx.Paths.ConfigPath, Config.ConfigFilePath, out bool copied, out string error))
            {
                Logger.LogError(error);
                enabled = false;
                return;
            }
            if (copied) Config.Reload();
            planner = new ClientPlanner(new ClientConfig(Config));
            Logger.LogInfo("Buildheim loaded. Press End in a world to open the planner.");
        }

        private void Update() => planner?.Update();
        private void LateUpdate() => planner?.DrawProjection();
        private void OnDestroy() => planner?.Dispose();
    }
}
