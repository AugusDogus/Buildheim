using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuildTest
{
    [TestClass]
    public class AssistanceRangeTests
    {
        [TestMethod]
        public void PieceRadiusCoversTheOffsetAndTheGeometryAroundIt()
        {
            // A unit cube centred on its own origin, 3 m along X from the blueprint origin.
            float radius = AssistanceRange.PieceRadius(new Vector3(3, 0, 0),
                new Bounds(Vector3.zero, Vector3.one), Vector3.one);
            Assert.AreEqual(3f + new Vector3(0.5f, 0.5f, 0.5f).magnitude, radius, 0.0001f);

            // Geometry offset inside its own piece has to be covered too, not just the piece origin.
            float lopsided = AssistanceRange.PieceRadius(Vector3.zero,
                new Bounds(new Vector3(0, 4, 0), Vector3.one), Vector3.one);
            Assert.IsTrue(lopsided > 4f, "A piece whose mesh sits away from its origin must enlarge the radius.");
        }

        [TestMethod]
        public void ScaledPiecesEnlargeTheRadiusByTheirLongestAxis()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            float plain = AssistanceRange.PieceRadius(Vector3.zero, bounds, Vector3.one);
            float scaled = AssistanceRange.PieceRadius(Vector3.zero, bounds, new Vector3(1, 1, 4));
            Assert.AreEqual(plain * 4f, scaled, 0.0001f);
        }

        [TestMethod]
        public void InRangeAddsTheBlueprintExtentTheHammerReachAndTheMargin()
        {
            var origin = Vector3.zero;
            const float radius = 10f, reach = 5f;
            float limit = radius + reach + AssistanceRange.Margin;

            Assert.IsTrue(AssistanceRange.InRange(new Vector3(limit - 0.1f, 0, 0), origin, radius, reach));
            Assert.IsFalse(AssistanceRange.InRange(new Vector3(limit + 0.1f, 0, 0), origin, radius, reach),
                "Assistance must release the hammer once the player leaves the build site.");

            // The failure this exists to stop: a placement left enabled on the far side of the map.
            Assert.IsFalse(AssistanceRange.InRange(new Vector3(1000, 0, 1000), origin, radius, reach));
        }
    }
}
