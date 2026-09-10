using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class ClientBlueprints
    {
        private const string Floor = "wood_floor;Building;1;2;3;0;0;0;1;\"\";1;1;1";

        [TestMethod]
        public void LoadsLegacyDescriptionAndPieceSection()
        {
            Assert.IsTrue(BlueprintDocument.TryParse("box", File.ReadLines("resources/TestBox_V2.blueprint"), false,
                out var document, out var error), error);
            Assert.AreEqual("Custom Name", document.Name);
            Assert.AreEqual(6, document.Pieces.Count);
        }

        [TestMethod]
        public void TerrainAndSnapMarkersAreNotBuildingInstructions()
        {
            string[] lines = { "#Name: House", "#Terrain", "unsupported terrain metadata",
                "#SnapPoints", "1;2;3", "#Pieces", Floor };
            Assert.IsTrue(BlueprintDocument.TryParse("house", lines, false, out var document, out var error), error);
            Assert.AreEqual(1, document.Pieces.Count);
            Assert.AreEqual("wood_floor", document.Pieces[0].name);
        }

        [TestMethod]
        public void RoundTripPreservesPoseUnderCommaDecimalCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                string entry = "wood_wall;Building;1.5;2.25;-3;0;0.7071068;0;0.7071068;\"\";1;2;1";
                Assert.IsTrue(BlueprintDocument.TryParse("house", new[] { entry }, false, out var first, out var error), error);
                Assert.IsTrue(BlueprintDocument.TryParse("copy", first.Serialize(), false, out var second, out error), error);
                Assert.AreEqual(1.5f, second.Pieces[0].posX);
                Assert.AreEqual(2.25f, second.Pieces[0].posY);
                Assert.AreEqual(-3f, second.Pieces[0].posZ);
                Assert.AreEqual(first.Pieces[0].rotY, second.Pieces[0].rotY, 0.00001f);
                Assert.AreEqual(2f, second.Pieces[0].scaleY);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [DataTestMethod]
        [DataRow("wood_floor;Building;NaN;0;0;0;0;0;1;\"\";1;1;1")]
        [DataRow("wood_floor;Building;Infinity;0;0;0;0;0;1;\"\";1;1;1")]
        [DataRow("wood_floor;Building;0;0;0;0;0;0;1;\"\";0;1;1")]
        [DataRow("wood_floor;Building;1")]
        public void InvalidPieceRejectsTheWholeDocument(string invalid)
        {
            Assert.IsFalse(BlueprintDocument.TryParse("broken", new[] { Floor, invalid }, false, out var document, out var error));
            Assert.IsNull(document);
            StringAssert.Contains(error, "line 2");
        }

        [TestMethod]
        public void EmptyAndOversizedBlueprintsHaveActionableErrors()
        {
            Assert.IsFalse(BlueprintDocument.TryParse("empty", Array.Empty<string>(), false, out _, out var emptyError));
            StringAssert.Contains(emptyError, "no building pieces");
            Assert.IsFalse(BlueprintDocument.TryParse("large", Enumerable.Repeat(Floor, BlueprintDocument.MaxPieces + 1),
                false, out _, out var largeError));
            StringAssert.Contains(largeError, "Split it");
        }
    }
}
