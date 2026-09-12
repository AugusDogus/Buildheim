using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class ClientPathTests
    {
        private string root;
        [TestInitialize] public void Setup() { root = Path.Combine(Path.GetTempPath(), "buildheim-path-test-" + Guid.NewGuid()); Directory.CreateDirectory(root); }
        [TestCleanup] public void Cleanup() => Directory.Delete(root, true);

        [TestMethod]
        public void FreshInstallUsesBuildheimAndUpgradeKeepsExistingBlueprintsAndPlacements()
        {
            Assert.AreEqual(Path.Combine(root, "Buildheim"), ClientPaths.DataDirectory(root));
            string legacy = Path.Combine(root, "PlanBuild");
            Directory.CreateDirectory(Path.Combine(legacy, "placements"));
            File.WriteAllText(Path.Combine(legacy, "placements", "world-player.json"), "saved plan");
            Assert.AreEqual(legacy, ClientPaths.DataDirectory(root));
            Assert.AreEqual("saved plan", File.ReadAllText(Path.Combine(ClientPaths.DataDirectory(root), "placements", "world-player.json")));
            Directory.CreateDirectory(Path.Combine(root, "Buildheim"));
            Assert.AreEqual(Path.Combine(root, "Buildheim"), ClientPaths.DataDirectory(root));
        }

        [TestMethod]
        public void SettingsAreCopiedOnceIncludingCustomPathsAndKeys()
        {
            string legacy = Path.Combine(root, "marcopogo.PlanBuild.cfg");
            string current = Path.Combine(root, "augusdogus.Buildheim.cfg");
            const string settings = "[Client]\nBlueprint directory = /my/custom/blueprints\nPlanner key = F9\nAutobuild key = F10\n";
            File.WriteAllText(legacy, settings);
            Assert.IsTrue(ClientPaths.TryMigrateConfig(root, current, out bool copied, out var error), error);
            Assert.IsTrue(copied);
            Assert.AreEqual(settings, File.ReadAllText(current));
            Assert.AreEqual(settings, File.ReadAllText(legacy));
            File.WriteAllText(current, "new preferences");
            Assert.IsTrue(ClientPaths.TryMigrateConfig(root, current, out copied, out error), error);
            Assert.IsFalse(copied);
            Assert.AreEqual("new preferences", File.ReadAllText(current));
        }

        [TestMethod]
        public void MissingLegacyConfigDoesNotCreateAnEmptyReplacement()
        {
            string current = Path.Combine(root, "augusdogus.Buildheim.cfg");
            Assert.IsTrue(ClientPaths.TryMigrateConfig(root, current, out bool copied, out var error), error);
            Assert.IsFalse(copied);
            Assert.IsFalse(File.Exists(current));
        }

        [TestMethod]
        public void FailedMigrationReportsTheProblemAndPreservesLegacySettings()
        {
            string legacy = Path.Combine(root, "marcopogo.PlanBuild.cfg");
            File.WriteAllText(legacy, "preferences");
            Assert.IsFalse(ClientPaths.TryMigrateConfig(root, Path.Combine(root, "missing", "settings.cfg"), out bool copied, out var error));
            Assert.IsFalse(copied);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
            Assert.AreEqual("preferences", File.ReadAllText(legacy));
        }
    }
}
