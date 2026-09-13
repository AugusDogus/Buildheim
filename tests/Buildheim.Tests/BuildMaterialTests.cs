using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class BuildMaterialTests
    {
        [TestMethod]
        public void ExactInventoryMaterialsPermitBuilding()
        {
            var costs = new Dictionary<string, int> { ["Wood"] = 4, ["Iron"] = 2 };
            Assert.IsTrue(BuildMaterials.HasAll(costs, item => costs[item]));
        }

        [TestMethod]
        public void MissingAnyMaterialPreventsBuilding()
        {
            var costs = new Dictionary<string, int> { ["Wood"] = 4, ["Iron"] = 2 };
            Assert.IsFalse(BuildMaterials.HasAll(costs, item => item == "Wood" ? 100 : 1));
        }

        [TestMethod]
        public void MissingListShowsOnlyTheAdditionalAmountsNeeded()
        {
            var costs = new Dictionary<string, int> { ["Fine wood"] = 5, ["Greydwarf eye"] = 5, ["Surtling core"] = 1 };
            var inventory = new Dictionary<string, int> { ["Fine wood"] = 2, ["Greydwarf eye"] = 20, ["Surtling core"] = 0 };
            var missing = BuildMaterials.Missing(costs, item => inventory[item]);
            Assert.AreEqual(2, missing.Count);
            Assert.AreEqual(3L, missing["Fine wood"]);
            Assert.AreEqual(1L, missing["Surtling core"]);
            Assert.IsFalse(missing.ContainsKey("Greydwarf eye"));
        }

        [TestMethod]
        public void MissingListAggregatesDuplicatesAndUpdatesWhenInventoryChanges()
        {
            var costs = new[] { new KeyValuePair<string, int>("Wood", 2), new KeyValuePair<string, int>("Wood", 3) };
            Assert.AreEqual(2L, BuildMaterials.Missing(costs, _ => 3)["Wood"]);
            Assert.AreEqual(0, BuildMaterials.Missing(costs, _ => 5).Count);
        }

        [TestMethod]
        public void DuplicateRequirementsCannotSpendTheSameInventoryTwice()
        {
            var costs = new[] { new KeyValuePair<string, int>("Wood", 2), new KeyValuePair<string, int>("Wood", 3) };
            Assert.IsFalse(BuildMaterials.HasAll(costs, _ => 3));
            Assert.IsTrue(BuildMaterials.HasAll(costs, _ => 5));
        }

        [TestMethod]
        public void OversizedCostsCannotOverflowIntoFreeBuilding()
        {
            var costs = new[] { new KeyValuePair<string, int>("Wood", int.MaxValue), new KeyValuePair<string, int>("Wood", 1) };
            Assert.IsFalse(BuildMaterials.HasAll(costs, _ => int.MaxValue));
        }
    }
}
