using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuildTest
{
    [TestClass]
    public class BlueprintHitTestTests
    {
        private static BlueprintHitTest Triangle() => new BlueprintHitTest(new[]
        {
            new Vector3(0, 0, 2), new Vector3(2, 0, 2), new Vector3(0, 2, 2)
        }, new[] { 0, 1, 2 });

        [TestMethod]
        public void EmptySpaceInsideTheBoundingBoxIsNotATarget()
        {
            Assert.IsFalse(Triangle().Intersect(new Ray(new Vector3(1.5f, 1.5f, 0), Vector3.forward), out _));
            Assert.IsTrue(Triangle().Intersect(new Ray(new Vector3(0.5f, 0.5f, 0), Vector3.forward), out float distance));
            Assert.AreEqual(2f, distance, 0.0001f);
        }

        [TestMethod]
        public void BothSidesAreSelectableButGeometryBehindTheCameraIsNot()
        {
            Assert.IsTrue(Triangle().Intersect(new Ray(new Vector3(0.5f, 0.5f, 3), Vector3.back), out float distance));
            Assert.AreEqual(1f, distance, 0.0001f);
            Assert.IsFalse(Triangle().Intersect(new Ray(new Vector3(0.5f, 0.5f, 3), Vector3.forward), out _));
            Assert.IsFalse(Triangle().Intersect(new Ray(Vector3.zero, Vector3.right), out _));
        }

        [TestMethod]
        public void LookingUpSelectsTheUndersideOfAnElevatedPiece()
        {
            var ceiling = new BlueprintHitTest(new[]
            {
                new Vector3(-2, 4, -2), new Vector3(2, 4, -2), new Vector3(0, 4, 2)
            }, new[] { 0, 1, 2 });
            Assert.IsTrue(ceiling.Intersect(new Ray(new Vector3(0, 1.5f, 0), Vector3.up), out float distance));
            Assert.AreEqual(2.5f, distance, 0.0001f);
        }

        [TestMethod]
        public void NearestTriangleWinsRegardlessOfMeshOrder()
        {
            var mesh = new BlueprintHitTest(new[]
            {
                new Vector3(0, 0, 4), new Vector3(2, 0, 4), new Vector3(0, 2, 4),
                new Vector3(0, 0, 2), new Vector3(2, 0, 2), new Vector3(0, 2, 2)
            }, new[] { 0, 1, 2, 3, 4, 5 });
            Assert.IsTrue(mesh.Intersect(new Ray(new Vector3(0.5f, 0.5f, 0), Vector3.forward), out float distance));
            Assert.AreEqual(2f, distance, 0.0001f);
        }
    }
}
