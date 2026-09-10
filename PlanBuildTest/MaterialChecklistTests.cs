using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class MaterialChecklistTests
    {
        private static Dictionary<string, int> Wood(int amount) => new Dictionary<string, int> { ["Wood"] = amount };

        [TestMethod]
        public void ReopeningAChestReplacesItsCountAndTransfersAreNotCountedTwice()
        {
            var checklist = new MaterialChecklist();
            checklist.Observe("chest-1", Wood(20));
            checklist.Observe("chest-1", Wood(12));
            var row = checklist.Rows(Wood(40), Wood(8)).Single();
            Assert.AreEqual(12L, row.Stored);
            Assert.AreEqual(20L, row.Missing);
        }

        [TestMethod]
        public void SeparateChestsAndStacksAreSummedWithoutOverflow()
        {
            var checklist = new MaterialChecklist();
            checklist.Observe("one", Wood(12));
            checklist.Observe("two", Wood(8));
            var costs = new[] { new KeyValuePair<string, int>("Wood", int.MaxValue), new KeyValuePair<string, int>("Wood", 1) };
            var row = checklist.Rows(costs, Wood(3)).Single();
            Assert.AreEqual(2147483648L, row.Required);
            Assert.AreEqual(20L, row.Stored);
            Assert.AreEqual(2147483625L, row.Missing);
        }

        [TestMethod]
        public void BulkCheckingIsANoteAndNeverChangesMaterialCounts()
        {
            var checklist = new MaterialChecklist();
            checklist.CheckAll(checklist.Rows(Wood(40), Wood(0)));
            var row = checklist.Rows(Wood(40), Wood(0)).Single();
            Assert.IsTrue(row.Checked);
            Assert.AreEqual(40L, row.Missing);
            checklist.ClearChecks();
            Assert.IsFalse(checklist.Rows(Wood(40), Wood(0)).Single().Checked);
        }

        [TestMethod]
        public void SurplusDoesNotBecomeNegativeAndForgettingChestsClearsAvailability()
        {
            var checklist = new MaterialChecklist();
            checklist.Observe("one", Wood(100));
            Assert.AreEqual(0L, checklist.Rows(Wood(40), Wood(5)).Single().Missing);
            checklist.ForgetChests();
            Assert.AreEqual(35L, checklist.Rows(Wood(40), Wood(5)).Single().Missing);
            Assert.AreEqual(0, checklist.ChestCount);
        }
    }
}
