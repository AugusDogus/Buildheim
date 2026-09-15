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
            var layers = new BlueprintLayers(new[] { 0f });
            Assert.IsFalse(AssistanceWork.HasWork(9, _ => (true, 0f), layers));
            Assert.IsFalse(AssistanceWork.HasWork(0, _ => (false, 0f), layers), "An empty blueprint is not work.");
        }

        [TestMethod]
        public void OneUnbuiltPieceIsEnoughToKeepAssisting()
        {
            var layers = new BlueprintLayers(new[] { 0f });
            Assert.IsTrue(AssistanceWork.HasWork(9, index => (index != 8, 0f), layers), "The last piece counts.");
            Assert.IsTrue(AssistanceWork.HasWork(9, index => (index != 0, 0f), layers));
        }

        [TestMethod]
        public void FinishingTheSelectedLayerReleasesAssistanceUntilAnotherLayerIsSelected()
        {
            var pieces = new[] { (Completed: false, Height: 0f), (Completed: false, Height: 4f) };
            var layers = new BlueprintLayers(new[] { 0f, 4f });
            layers.Bottom();
            Assert.IsTrue(AssistanceWork.HasWork(pieces.Length, index => pieces[index], layers));

            pieces[0] = (true, 0f);
            Assert.IsFalse(AssistanceWork.HasWork(pieces.Length, index => pieces[index], layers),
                "Unfinished pieces outside the selected layer must not keep assistance engaged.");

            layers.Move(1);
            Assert.IsTrue(AssistanceWork.HasWork(pieces.Length, index => pieces[index], layers));
            layers.Move(-1);
            Assert.IsFalse(AssistanceWork.HasWork(pieces.Length, index => pieces[index], layers));
            layers.All();
            Assert.IsTrue(AssistanceWork.HasWork(pieces.Length, index => pieces[index], layers));
        }

        [TestMethod]
        public void AnUnbuiltPieceReactivatesACompletedLayer()
        {
            var layers = new BlueprintLayers(new[] { 0f });
            layers.Bottom();
            bool completed = true;
            Assert.IsFalse(AssistanceWork.HasWork(1, _ => (completed, 0f), layers));
            completed = false;
            Assert.IsTrue(AssistanceWork.HasWork(1, _ => (completed, 0f), layers));
        }

        [TestMethod]
        public void ItStopsAtTheFirstBuildablePieceRatherThanScanningTheWholeBlueprint()
        {
            // Read every frame, so the common case must not walk a large blueprint.
            var visited = new List<int>();
            var layers = new BlueprintLayers(new[] { 0f, 4f });
            layers.Bottom();
            Assert.IsTrue(AssistanceWork.HasWork(500, index =>
            {
                visited.Add(index);
                return (index == 0, index == 1 ? 4f : 0f);
            }, layers));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, visited);
        }
    }
}
