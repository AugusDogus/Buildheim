using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class AutoBuildQueueTests
    {
        [TestMethod]
        public void TargetStaysVisibleUntilItsPacedAttempt()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(3, 0f, _ => true));
            Assert.AreEqual(0, queue.Next(3, 0.1f, _ => true));
            Assert.IsFalse(queue.TryAttempt(0.1f));
            Assert.IsTrue(queue.TryAttempt(0.5f));
            Assert.IsFalse(queue.TryAttempt(0.5f));
            Assert.AreEqual(1, queue.Next(3, 0.5f, _ => true));
            Assert.IsFalse(queue.TryAttempt(0.5f));
        }

        [TestMethod]
        public void LosingMaterialsSelectsAnotherPieceBeforeAttempting()
        {
            var queue = new AutoBuildQueue();
            int wood = 2;
            bool Eligible(int index) => index == 0 ? wood >= 2 : true;
            Assert.AreEqual(0, queue.Next(2, 0f, Eligible));
            wood = 0;
            Assert.AreEqual(1, queue.Next(2, 0.1f, Eligible));
            Assert.IsFalse(queue.TryAttempt(0.5f));
            Assert.IsTrue(queue.TryAttempt(0.6f));
        }

        [TestMethod]
        public void ChangingLayerDiscardsThePreviousTarget()
        {
            var queue = new AutoBuildQueue();
            var heights = new[] { 4f, 0f, 2f };
            var layers = new BlueprintLayers(heights);
            Assert.AreEqual(0, queue.Next(3, 0f, i => layers.Contains(heights[i])));
            layers.Bottom();
            Assert.AreEqual(1, queue.Next(3, 0.1f, i => layers.Contains(heights[i])));
            layers.Move(1);
            Assert.AreEqual(2, queue.Next(3, 0.2f, i => layers.Contains(heights[i])));
        }

        [TestMethod]
        public void FailedAttemptsAdvanceToOtherEligiblePieces()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(1, queue.Next(3, 0f, i => i != 0));
            Assert.IsTrue(queue.TryAttempt(0.5f));
            Assert.AreEqual(2, queue.Next(3, 0.5f, i => i != 0));
            Assert.IsTrue(queue.TryAttempt(1f));
            Assert.AreEqual(1, queue.Next(3, 1f, i => i != 0));
        }

        [TestMethod]
        public void NoEligiblePiecesClearsSelectionAndCannotAttempt()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(2, 0f, _ => true));
            Assert.IsNull(queue.Next(2, 0.1f, _ => false));
            Assert.IsFalse(queue.TryAttempt(1f));
            Assert.IsNull(queue.Next(0, 1f, _ => { Assert.Fail("Empty blueprint"); return false; }));
        }

        [TestMethod]
        public void ResetDiscardsTimingAndCandidateState()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(3, 100f, _ => true));
            queue.Reset();
            Assert.IsFalse(queue.TryAttempt(101f));
            Assert.AreEqual(0, queue.Next(1, 0f, _ => true));
            Assert.IsTrue(queue.TryAttempt(0.5f));
        }
    }
}
