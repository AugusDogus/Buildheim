using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PlanBuildTest
{
    // These checks inspect the installed game, without starting Unity or modifying its assemblies.
    // They detect changes to the vanilla placement contract on which assistance relies.
    [TestClass]
    public class HammerContract
    {
        [TestMethod]
        public void VanillaClickConsumesResourcesOnlyAfterPlacementSucceeds()
        {
            using var game = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"));
            var player = game.MainModule.Types.Single(x => x.Name == "Player");
            var update = player.Methods.Single(x => x.Name == "UpdatePlacement");
            var calls = update.Body.Instructions.Where(x => x.Operand is MethodReference)
                .Select(x => ((MethodReference)x.Operand).Name).ToList();
            CollectionAssert.Contains(calls, "HaveRequirements");
            CollectionAssert.Contains(calls, "TryPlacePiece");
            CollectionAssert.Contains(calls, "ConsumeResources");
            Assert.IsTrue(calls.IndexOf("HaveRequirements") < calls.IndexOf("TryPlacePiece"));
            Assert.IsTrue(calls.IndexOf("TryPlacePiece") < calls.IndexOf("ConsumeResources"));
            var build = update.Body.Instructions.Single(x => x.Operand is MethodReference method && method.Name == "TryPlacePiece");
            var consume = update.Body.Instructions.Single(x => x.Operand is MethodReference method && method.Name == "ConsumeResources");
            Assert.IsTrue(build.Next.OpCode == OpCodes.Brfalse || build.Next.OpCode == OpCodes.Brfalse_S,
                "Failed placement must branch past resource consumption.");
            Assert.IsTrue(build.Next.Operand is Instruction destination && destination.Offset > consume.Offset);
            var attempt = player.Methods.Single(x => x.Name == "TryPlacePiece");
            var attemptCalls = attempt.Body.Instructions.Where(x => x.Operand is MethodReference)
                .Select(x => ((MethodReference)x.Operand).Name).ToList();
            CollectionAssert.Contains(attemptCalls, "UpdatePlacementGhost");
            CollectionAssert.Contains(attemptCalls, "PlacePiece");
            Assert.IsTrue(attemptCalls.IndexOf("UpdatePlacementGhost") < attemptCalls.IndexOf("PlacePiece"));
        }

        [TestMethod]
        public void GhostPlacementUsesInterceptableSettersAndRetainsWorldChecks()
        {
            using var game = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"));
            var player = game.MainModule.Types.Single(x => x.Name == "Player");
            var ghost = player.Methods.Single(x => x.Name == "UpdatePlacementGhost");
            var methods = ghost.Body.Instructions.Where(x => x.Operand is MethodReference)
                .Select(x => (MethodReference)x.Operand).ToList();
            Assert.IsTrue(methods.Any(x => x.DeclaringType.Name == "Transform" && x.Name == "set_position"));
            Assert.IsTrue(methods.Any(x => x.DeclaringType.Name == "Transform" && x.Name == "set_rotation"));
            Assert.IsFalse(methods.Any(x => x.DeclaringType.Name == "Transform" &&
                (x.Name == "SetPositionAndRotation" || x.Name == "set_localPosition" || x.Name == "set_localRotation")),
                "New transform writes would escape the assistance hooks.");
            Assert.IsTrue(methods.Any(x => x.Name == "PieceRayTest"));
            Assert.IsTrue(methods.Any(x => x.DeclaringType.Name == "PrivateArea" && x.Name == "CheckAccess"));
            Assert.IsTrue(methods.Any(x => x.Name == "IsInsideNoBuildLocation"));
            Assert.IsTrue(methods.Any(x => x.Name == "CheckPlacementGhostVSPlayers"));
            Assert.IsTrue(methods.Any(x => x.Name == "TestGhostClipping"));
        }
    }
}
