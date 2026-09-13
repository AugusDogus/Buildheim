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
        public void BlueprintSelectionCanRejectStaleRecipesBeforeVanillaChecksCosts()
        {
            using var game = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"));
            var update = game.MainModule.Types.Single(x => x.Name == "Player").Methods.Single(x => x.Name == "UpdatePlacement");
            var recipe = update.Body.Instructions.Single(x => x.Operand is MethodReference method &&
                method.DeclaringType.Name == "PieceTable" && method.Name == "GetSelectedPiece");
            var requirement = update.Body.Instructions.Single(x => x.Operand is MethodReference method && method.Name == "HaveRequirements");
            Assert.IsTrue(recipe.Offset < requirement.Offset);
            Assert.IsTrue(update.Body.Instructions.Any(x => x.Offset > recipe.Offset && x.Offset < requirement.Offset &&
                (x.OpCode == OpCodes.Brfalse || x.OpCode == OpCodes.Brfalse_S) &&
                x.Operand is Instruction destination && destination.Offset > requirement.Offset),
                "A null recipe must skip vanilla's requirement check and build attempt.");
        }

        [TestMethod]
        public void VanillaReachIsMeasuredToTheRayHitRatherThanThePieceOrigin()
        {
            using var game = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"));
            var ray = game.MainModule.Types.Single(x => x.Name == "Player").Methods.Single(x => x.Name == "PieceRayTest");
            var code = ray.Body.Instructions;
            var distance = code.Single(x => x.Operand is MethodReference method && method.DeclaringType.Name == "Vector3" && method.Name == "Distance");
            Assert.IsTrue(code.Any(x => x.Offset < distance.Offset && x.Operand is FieldReference field && field.Name == "m_eye"));
            Assert.IsTrue(distance.Previous.Operand is MethodReference point && point.DeclaringType.Name == "RaycastHit" && point.Name == "get_point",
                "Vanilla's range check must measure to the actual hit surface.");
            Assert.IsTrue(code.Any(x => x.Operand is FieldReference field && field.Name == "m_extraPlacementDistance"));
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

        [TestMethod]
        public void PositioningCanConsumeWheelInputBeforeHammerRotationAndCameraZoom()
        {
            using var game = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_valheim.dll"));
            using var input = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "assembly_utils.dll"));
            var zinput = input.MainModule.Types.Single(type => type.Name == "ZInput");
            var wheel = zinput.Methods.Single(method => method.Name == "GetMouseScrollWheel");
            Assert.IsTrue(wheel.IsStatic);
            Assert.AreEqual("System.Single", wheel.ReturnType.FullName);
            var button = zinput.Methods.Single(method => method.Name == "GetButtonDown");
            Assert.AreEqual("name", button.Parameters.Single().Name);
            Assert.AreEqual("System.String", button.Parameters.Single().ParameterType.FullName);
            foreach (string type in new[] { "Player", "GameCamera" })
                Assert.IsTrue(game.MainModule.Types.Single(t => t.Name == type).Methods
                    .Where(method => method.HasBody).SelectMany(method => method.Body.Instructions)
                    .Any(instruction => instruction.Operand is MethodReference method &&
                        method.DeclaringType.Name == "ZInput" && method.Name == "GetMouseScrollWheel"), type);
        }
    }
}
