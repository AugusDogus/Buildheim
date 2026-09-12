using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class BlueprintRotationTests
    {
        [DataTestMethod]
        [DataRow(0f, 1, false, 22.5f)]
        [DataRow(0f, -1, false, 337.5f)]
        [DataRow(337.5f, 1, false, 0f)]
        [DataRow(1f, 1, false, 22.5f)]
        [DataRow(1f, -1, false, 0f)]
        [DataRow(359f, 1, false, 0f)]
        [DataRow(-1f, -1, false, 337.5f)]
        [DataRow(45f, 1, true, 135f)]
        [DataRow(45f, -1, true, 315f)]
        public void TurningAlwaysLandsOnTheHammerRotationGrid(float yaw, int direction, bool coarse, float expected)
        {
            Assert.AreEqual(expected, BlueprintRotation.Turn(yaw, direction, coarse), 0.0001f);
        }
    }
}
