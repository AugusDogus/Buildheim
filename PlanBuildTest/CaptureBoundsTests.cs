using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class CaptureBoundsTests
    {
        [TestMethod]
        public void ReversedCornersIncludeBoundaryPiecesButExcludeNeighborsAndOtherFloors()
        {
            Assert.IsTrue(CaptureBounds.TryCreate(10, -2, 20, -10, 6, -20, out var box));
            Assert.IsTrue(box.HasVolume);
            Assert.AreEqual(20f, box.Width);
            Assert.AreEqual(8f, box.Height);
            Assert.AreEqual(40f, box.Depth);
            Assert.IsTrue(box.Contains(0, 0, 0));
            Assert.IsTrue(box.Contains(-10, -2, -20));
            Assert.IsTrue(box.Contains(10, 6, 20));
            Assert.IsFalse(box.Contains(10.1f, 0, 0));
            Assert.IsFalse(box.Contains(0, 6.1f, 0));
            Assert.IsFalse(box.Contains(0, -2.1f, 0));
            Assert.IsFalse(box.Contains(0, 0, -20.1f));
        }

        [TestMethod]
        public void FlatSelectionCanBePreviewedButNeedsHeightBeforeCapture()
        {
            Assert.IsTrue(CaptureBounds.TryCreate(0, 3, 0, 10, 3, 10, out var flat));
            Assert.IsFalse(flat.HasVolume);
            Assert.IsTrue(CaptureBounds.TryCreate(0, 3, 0, 10, 8, 10, out var raised));
            Assert.IsTrue(raised.HasVolume);
            Assert.IsTrue(raised.Contains(5, 7, 5));
        }

        [DataTestMethod]
        [DataRow(float.NaN)]
        [DataRow(float.PositiveInfinity)]
        [DataRow(float.NegativeInfinity)]
        public void NonFiniteCornersCannotCreateASelection(float invalid)
        {
            Assert.IsFalse(CaptureBounds.TryCreate(0, 0, 0, invalid, 1, 1, out var box));
            Assert.IsNull(box);
        }
    }
}
