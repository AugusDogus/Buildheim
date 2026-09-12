using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class AutoBuildQueueTests
    {
        [TestMethod]
        public void AttemptsArePacedEvenWhenTheLastPieceCannotBePlaced()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(3, 0f, _ => true));
            Assert.IsNull(queue.Next(3, 0.1f, _ => true));
            Assert.AreEqual(1, queue.Next(3, 0.5f, _ => true));
        }

        [TestMethod]
        public void IneligiblePiecesDoNotBlockLaterCandidates()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(2, queue.Next(4, 0f, index => index == 2));
            Assert.AreEqual(3, queue.Next(4, 1f, _ => true));
        }

        [TestMethod]
        public void FailedCandidatesAreRetriedAfterOtherPieces()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(2, 0f, _ => true));
            Assert.AreEqual(1, queue.Next(2, 1f, _ => true));
            Assert.AreEqual(0, queue.Next(2, 2f, _ => true));
        }

        [TestMethod]
        public void EmptyOrUnbuildableBlueprintDoesNotQueuePlacement()
        {
            var queue = new AutoBuildQueue();
            Assert.IsNull(queue.Next(0, 0f, _ => { Assert.Fail("Empty blueprints cannot have candidates."); return false; }));
            Assert.IsNull(queue.Next(4, 1f, _ => false));
            Assert.IsNull(queue.Next(4, 1.1f, _ => true));
        }

        [TestMethod]
        public void ResetDiscardsTimingAndCandidateState()
        {
            var queue = new AutoBuildQueue();
            Assert.AreEqual(0, queue.Next(3, 100f, _ => true));
            queue.Reset();
            Assert.AreEqual(0, queue.Next(1, 0f, _ => true));
        }
    }
}
