using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;

namespace PlanBuildTest
{
    [TestClass]
    public class PlacementSaveTests
    {
        private string directory;
        [TestInitialize] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "planbuild-save-test-" + Guid.NewGuid()); Directory.CreateDirectory(directory); }
        [TestCleanup] public void Cleanup() => Directory.Delete(directory, true);

        private static PlacementSave Sample() => new PlacementSave
        {
            Version = 1, Name = "House", Blueprint = new[] { "#Name:House", "#Pieces", "wood_wall;Building;0;0;0;0;0;0;1;;1;1;1" },
            X = 123.25f, Y = -4.5f, Z = 678, Yaw = 22.5f, LayerHeight = 2, Layer = 0,
            PreviewOnly = true, CheckedMaterials = new[] { "$item_wood" }
        };

        [TestMethod]
        public void PlacementAndGatheringNotesRoundTripAndCanBeUpdated()
        {
            string path = PlacementStore.PathFor(directory, 111, 222);
            var original = Sample();
            Assert.IsTrue(PlacementStore.TryWrite(path, original, out var error), error);
            Assert.IsTrue(PlacementStore.TryRead(path, out var read, out error), error);
            Assert.AreEqual(original.X, read.X);
            Assert.AreEqual(original.Y, read.Y);
            Assert.AreEqual(original.Z, read.Z);
            Assert.AreEqual(original.Yaw, read.Yaw);
            Assert.AreEqual(original.Layer, read.Layer);
            Assert.IsTrue(read.PreviewOnly);
            CollectionAssert.AreEqual(original.CheckedMaterials, read.CheckedMaterials);
            CollectionAssert.AreEqual(original.Blueprint, read.Blueprint);
            original.Y = 42;
            Assert.IsTrue(PlacementStore.TryWrite(path, original, out error), error);
            Assert.IsTrue(PlacementStore.TryRead(path, out read, out error), error);
            Assert.AreEqual(42f, read.Y);
        }

        [TestMethod]
        public void WorldsAndCharactersHaveSeparateSavesAndClearStaysCleared()
        {
            string path = PlacementStore.PathFor(directory, 111, 222);
            Assert.IsTrue(PlacementStore.TryWrite(path, Sample(), out _));
            Assert.IsFalse(PlacementStore.TryRead(PlacementStore.PathFor(directory, 333, 222), out _, out _));
            Assert.IsFalse(PlacementStore.TryRead(PlacementStore.PathFor(directory, 111, 444), out _, out _));
            Assert.IsTrue(PlacementStore.TryClear(path, out _));
            Assert.IsFalse(PlacementStore.TryRead(path, out _, out var error));
            Assert.AreEqual("", error);
        }

        [TestMethod]
        public void InvalidTransformsCannotReplaceAGoodSave()
        {
            string path = PlacementStore.PathFor(directory, 111, 222);
            Assert.IsTrue(PlacementStore.TryWrite(path, Sample(), out _));
            string original = File.ReadAllText(path);
            var invalid = Sample();
            invalid.Yaw = float.NaN;
            Assert.IsFalse(PlacementStore.TryWrite(path, invalid, out _));
            Assert.AreEqual(original, File.ReadAllText(path));
        }

        [DataTestMethod]
        [DataRow("{")]
        [DataRow("null")]
        [DataRow("{}")]
        [DataRow("[]")]
        [DataRow("{\"Version\":\"bad\"}")]
        public void CorruptSavesAreReportedAndPreserved(string contents)
        {
            string path = PlacementStore.PathFor(directory, 111, 222);
            File.WriteAllText(path, contents);
            Assert.IsFalse(PlacementStore.TryRead(path, out _, out var error));
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
            Assert.AreEqual(contents, File.ReadAllText(path));
        }

        [TestMethod]
        public void RestoredLayerMustExistAndCannotCorruptCurrentSelection()
        {
            var layers = new BlueprintLayers(new[] { 0f, 4f });
            Assert.IsTrue(layers.Select(2));
            Assert.AreEqual(2, layers.Selected);
            Assert.IsFalse(layers.Select(100));
            Assert.AreEqual(2, layers.Selected);
            Assert.IsTrue(layers.Select(null));
            Assert.IsNull(layers.Selected);
        }
    }
}
