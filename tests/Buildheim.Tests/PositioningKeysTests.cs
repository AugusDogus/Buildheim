using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlanBuild.Client;
using UnityEngine;

namespace PlanBuildTest
{
    [TestClass]
    public class PositioningKeysTests
    {
        [TestMethod]
        public void DefaultHintsMatchTheOriginalShortcuts()
        {
            Assert.AreEqual("Ctrl + wheel: forward/back   |   Ctrl + X + wheel: sideways\n" +
                "Alt + wheel: height   |   Ctrl + Alt + wheel: rotate   |   Shift: larger steps",
                PositioningKeys.Default.ProjectionHints);
            Assert.AreEqual("Left click: corner A | Right click: corner B | Middle click: switch corner\n" +
                "Ctrl + wheel: forward/back | Ctrl + X + wheel: sideways | Alt + wheel: height | Shift: 1 m",
                PositioningKeys.Default.CaptureHints);
        }

        [TestMethod]
        public void HintsNameTheConfiguredKeys()
        {
            var keys = new PositioningKeys(new ModifierKey(KeyCode.Mouse3, KeyCode.None),
                new ModifierKey(KeyCode.None, KeyCode.RightAlt), KeyCode.Z,
                new ModifierKey(KeyCode.LeftShift, KeyCode.Tab));
            Assert.AreEqual("Mouse3 + wheel: forward/back   |   Mouse3 + Z + wheel: sideways\n" +
                "RightAlt + wheel: height   |   Mouse3 + RightAlt + wheel: rotate   |   LeftShift/Tab: larger steps",
                keys.ProjectionHints);
        }

        [DataTestMethod]
        [DataRow(KeyCode.LeftControl, KeyCode.RightControl, "Ctrl")]
        [DataRow(KeyCode.RightShift, KeyCode.LeftShift, "Shift")]
        [DataRow(KeyCode.LeftAlt, KeyCode.LeftAlt, "LeftAlt")]
        [DataRow(KeyCode.LeftControl, KeyCode.RightAlt, "LeftControl/RightAlt")]
        [DataRow(KeyCode.None, KeyCode.None, "Unbound")]
        public void ModifierLabelsCombineOnlyMatchingLeftAndRightKeys(KeyCode primary, KeyCode alternate, string expected)
        {
            Assert.AreEqual(expected, new ModifierKey(primary, alternate).Label);
        }

        [TestMethod]
        public void ModifiersAreHeldByEitherBoundKeyButNeverByNone()
        {
            var modifier = new ModifierKey(KeyCode.LeftControl, KeyCode.None);
            Assert.IsTrue(modifier.Held(key => key == KeyCode.LeftControl));
            Assert.IsFalse(modifier.Held(key => key == KeyCode.RightControl));
            Assert.IsFalse(modifier.Held(key => key == KeyCode.None));
            Assert.IsTrue(new ModifierKey(KeyCode.None, KeyCode.RightControl).Held(key => key == KeyCode.RightControl));
            var unbound = new PositioningKeys(modifier, modifier, KeyCode.None, modifier);
            Assert.IsFalse(unbound.SidewaysHeld(key => key == KeyCode.None));
        }
    }
}
