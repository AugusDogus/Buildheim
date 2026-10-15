using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuildTest
{
    [TestClass]
    public class BlueprintSnappingTests
    {
        [TestMethod]
        public void NearestPointAlignsInAllThreeAxesWithoutChangingInternalOffsets()
        {
            var points = new[] { new Vector3(-4, 0, 0), new Vector3(2, 3, -1) };
            var origin = new Vector3(10, -2, 20);
            var target = new Vector3(12.1f, 1.1f, 19.1f);
            Assert.IsTrue(BlueprintSnapping.TryPosition(points, origin, Quaternion.identity, target, out var position));
            AssertNear(target, position + points[1]);
            AssertNear(points[1] - points[0], (position + points[1]) - (position + points[0]));
        }

        [TestMethod]
        public void NearestPointIsChosenAfterRotationAndTranslation()
        {
            var points = new[] { new Vector3(0, 0, -2), new Vector3(2, 1, 0) };
            // 90 degrees about Y, constructed directly so the test needs no Unity runtime.
            float half = (float)Math.Sqrt(0.5);
            var rotation = new Quaternion(0, half, 0, half);
            var origin = new Vector3(-20, 5, -30);
            var target = new Vector3(-20.1f, 6.2f, -32.1f);
            Assert.IsTrue(BlueprintSnapping.TryPosition(points, origin, rotation, target, out var position));
            AssertNear(target, position + rotation * points[1]);
            Assert.IsTrue(BlueprintSnapping.TryPosition(points, position, rotation, target, out var repeated));
            AssertNear(position, repeated);
        }

        [TestMethod]
        public void MissingPointsLeaveThePositionUnchanged()
        {
            var origin = new Vector3(1, 2, 3);
            Assert.IsFalse(BlueprintSnapping.TryPosition(Array.Empty<Vector3>(), origin, Quaternion.identity,
                new Vector3(10, 20, 30), out var position));
            Assert.AreEqual(origin, position);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}
