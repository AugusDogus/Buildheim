using BepInEx;
using Jotunn.Utils;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuild
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, "2.30.0")]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    internal class PlanBuildPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "marcopogo.PlanBuild";
        public const string PluginName = "PlanBuild";
        public const string PluginVersion = "0.19.0";

        private ClientPlanner planner;

        private void Awake()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            planner = new ClientPlanner(new ClientConfig(Config));
            Logger.LogInfo("Client blueprint planner loaded. Press End in a world to open it.");
        }

        private void Update() => planner?.Update();
        private void LateUpdate() => planner?.DrawProjection();
        private void OnGUI() => planner?.DrawWindow();
        private void OnDestroy() => planner?.Dispose();
    }
}
