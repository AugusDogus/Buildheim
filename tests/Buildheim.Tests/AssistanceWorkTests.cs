using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class AssistanceWorkTests
    {
        [TestMethod]
        public void AFinishedBlueprintHasNoWorkLeft()
        {
            // The original bug: every piece built, so nothing to draw and nothing to target,
            // while assistance kept the hammer.
            Assert.IsFalse(AssistanceWork.HasWork(9, _ => false));
            Assert.IsFalse(AssistanceWork.HasWork(0, _ => true), "An empty blueprint is not work.");
        }

        [TestMethod]
        public void OneUnbuiltPieceIsEnoughToKeepAssisting()
        {
            Assert.IsTrue(AssistanceWork.HasWork(9, index => index == 8), "The last piece counts.");
            Assert.IsTrue(AssistanceWork.HasWork(9, index => index == 0));
        }

        [TestMethod]
        public void ItStopsAtTheFirstBuildablePieceRatherThanScanningTheWholeBlueprint()
        {
            // Read every frame, so the common case must not walk a large blueprint.
            var visited = new List<int>();
            Assert.IsTrue(AssistanceWork.HasWork(500, index => { visited.Add(index); return index >= 2; }));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, visited);
        }
    }
}
