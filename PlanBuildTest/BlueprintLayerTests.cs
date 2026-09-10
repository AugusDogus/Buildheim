using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class BlueprintLayerTests
    {
        [TestMethod]
        public void StartsWithAllPiecesVisible()
        {
            var layers = new BlueprintLayers(new[] { -1f, 0f, 2f });
            Assert.IsNull(layers.Selected);
            Assert.IsTrue(layers.Contains(-1f));
            Assert.IsTrue(layers.Contains(2f));
        }

        [TestMethod]
        public void BottomLayerIncludesNegativeHeightsWithoutLeakingTheNextBand()
        {
            var layers = new BlueprintLayers(new[] { -2f, -0.1f, 0f, 2f });
            layers.Bottom();
            Assert.IsTrue(layers.Contains(-2f));
            Assert.IsTrue(layers.Contains(-0.1f));
            Assert.IsFalse(layers.Contains(0f));
        }

        [TestMethod]
        public void MovingUpSkipsEmptyLayersAndStopsAtTheTop()
        {
            var layers = new BlueprintLayers(new[] { 0f, 6f });
            layers.Bottom();
            layers.Move(1);
            Assert.IsTrue(layers.Contains(6f));
            Assert.IsFalse(layers.Contains(0f));
            layers.Move(1);
            Assert.IsTrue(layers.Contains(6f));
            layers.Move(-1);
            Assert.IsTrue(layers.Contains(0f));
        }

        [TestMethod]
        public void ChangingBandHeightRebuildsLayersAndRestartsAtBottom()
        {
            var layers = new BlueprintLayers(new[] { 0f, 1f, 2f });
            layers.Bottom();
            Assert.IsTrue(layers.Contains(1f));
            Assert.IsTrue(layers.SetHeight(1f));
            Assert.IsFalse(layers.Contains(1f));
            layers.Move(1);
            Assert.IsTrue(layers.Contains(1f));
            layers.All();
            Assert.IsTrue(layers.Contains(0f));
            Assert.IsTrue(layers.Contains(2f));
        }

        [TestMethod]
        public void InvalidLayerHeightPreservesTheCurrentSelection()
        {
            var layers = new BlueprintLayers(new[] { 0f, 2f });
            layers.Bottom();
            layers.Move(1);
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                Assert.IsFalse(layers.SetHeight(invalid));
            Assert.AreEqual(2f, layers.Height);
            Assert.IsTrue(layers.Contains(2f));
        }

        [TestMethod]
        public void AutobuildCannotSelectOutsideTheActiveLayer()
        {
            float[] origins = { 4f, 0f, 2f };
            var layers = new BlueprintLayers(origins);
            var queue = new AutoBuildQueue();
            layers.Bottom();
            Assert.AreEqual(1, queue.Next(origins.Length, 0f, index => layers.Contains(origins[index])));
            layers.Move(1);
            Assert.AreEqual(2, queue.Next(origins.Length, 1f, index => layers.Contains(origins[index])));
            layers.Move(1);
            Assert.AreEqual(0, queue.Next(origins.Length, 2f, index => layers.Contains(origins[index])));
        }
    }
}
