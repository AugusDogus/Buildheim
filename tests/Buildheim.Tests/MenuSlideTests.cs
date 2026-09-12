using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class MenuSlideTests
    {
        [TestMethod]
        public void OpeningAndClosingReachTheirEndpointsEvenOnASlowFrame()
        {
            var slide = new MenuSlide();
            Assert.AreEqual(0f, slide.Amount);
            slide.Advance(true, 0.05f, 0.2f);
            Assert.AreEqual(0.25f, slide.Amount, 0.0001f);
            slide.Advance(true, 1, 0.2f);
            Assert.AreEqual(1f, slide.Amount);
            slide.Advance(false, 0.05f, 0.2f);
            Assert.AreEqual(0.75f, slide.Amount, 0.0001f);
            slide.Advance(false, 1, 0.2f);
            Assert.AreEqual(0f, slide.Amount);
        }

        [TestMethod]
        public void RapidToggleReversesFromTheCurrentPosition()
        {
            var slide = new MenuSlide();
            slide.Advance(true, 0.1f, 0.2f);
            Assert.AreEqual(0.5f, slide.Amount);
            slide.Advance(false, 0.02f, 0.2f);
            Assert.AreEqual(0.4f, slide.Amount, 0.0001f);
            slide.Advance(true, 0.02f, 0.2f);
            Assert.AreEqual(0.5f, slide.Amount, 0.0001f);
            slide.Reset();
            Assert.AreEqual(0f, slide.Amount);
        }
    }
}
