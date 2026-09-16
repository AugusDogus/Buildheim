using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanBuild.Client
{
    // Observe only modifiers, never typed text. These reads do not control rotation.
    internal sealed class RotationInputReport : IDisposable
    {
        private const int EventLimit = 2000;
        private static readonly KeyCode[] Modifiers = { KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt };
        private static readonly Key[] RawModifiers = { Key.LeftShift, Key.RightShift,
            Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt };
        private readonly Func<bool> enabled;
        private readonly string path = Path.Combine(Paths.BepInExRootPath, "Buildheim-input-report.txt");
        private StreamWriter writer;
        private string previous;
        private int events;
        private bool stopped;

        public RotationInputReport(Func<bool> enabled) { this.enabled = enabled; }

        private bool Enabled
        {
            get
            {
#if BUILDHEIM_INPUT_REPORT
                return !stopped;
#else
                return !stopped && enabled();
#endif
            }
        }

        public void Observe(bool projectionActive)
        {
            if (!Enabled) return;
            // Start when a projection becomes usable, after plugins have loaded.
            if (writer == null && !projectionActive) return;
            WriteSafely(() =>
            {
                if (writer == null) Start();
                string state = $"projection={projectionActive} {State()}";
                if (state == previous) return;
                previous = state;
                Event("state " + state);
            });
        }

        public void Rotation(float wheel, bool coarse, float before, float after)
        {
            if (!Enabled) return;
            WriteSafely(() =>
            {
                if (writer == null) Start();
                Event(FormattableString.Invariant(
                    $"rotation wheel={wheel:R} coarse={coarse} yaw={before:R}->{after:R} delta={Mathf.DeltaAngle(before, after):R} ") + State());
            });
        }

        private void Start()
        {
            writer = new StreamWriter(path, false) { AutoFlush = true };
            writer.WriteLine($"Buildheim input report: build={typeof(RotationInputReport).Assembly.ManifestModule.ModuleVersionId} utc={DateTime.UtcNow:O}");
            writer.WriteLine($"OS={SystemInfo.operatingSystem}; Unity={Application.unityVersion}");
            writer.WriteLine("Modifier bits, left to right: ShiftL ShiftR CtrlL CtrlR AltL AltR. 1=down, 0=up.");
            writer.WriteLine("Windows is GetAsyncKeyState, not a physical hardware trace; unavailable/unfocused input may read as up.");
            writer.WriteLine($"Only modifier transitions and rotations are recorded, capped at {EventLimit} events. No typed text is recorded. This file is replaced next session.");
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var sticky = new StickyKeys { Size = (uint)Marshal.SizeOf(typeof(StickyKeys)) };
                bool available = SystemParametersInfo(0x003A, sticky.Size, ref sticky, 0);
                writer.WriteLine($"Windows keyboardLayout={GetKeyboardLayout(0).ToInt64():X}; stickyKeysFlags={(available ? sticky.Flags.ToString("X") : "unavailable")}");
            }
            foreach (var plugin in Chainloader.PluginInfos.Values.OrderBy(p => p.Metadata.GUID))
                writer.WriteLine($"mod={plugin.Metadata.GUID} version={plugin.Metadata.Version} name={plugin.Metadata.Name}");
            foreach (var device in InputSystem.devices)
                writer.WriteLine($"device id={device.deviceId} layout={device.layout} name={device.name} interface={device.description.interfaceName} product={device.description.product}");
            foreach (var method in Harmony.GetAllPatchedMethods().Where(m =>
                m.DeclaringType == typeof(ZInput) || m.DeclaringType == typeof(Input) ||
                (m.DeclaringType?.Namespace?.StartsWith("UnityEngine.InputSystem", StringComparison.Ordinal) ?? false))
                .OrderBy(m => m.DeclaringType.FullName).ThenBy(m => m.Name))
            {
                var patches = Harmony.GetPatchInfo(method);
                if (patches != null)
                    writer.WriteLine($"inputPatch={method.DeclaringType.FullName}.{method} owners={string.Join(",", patches.Owners)}");
            }
            Jotunn.Logger.LogInfo($"Buildheim input report enabled: {path}. Reproduce the rotation issue, then attach this file.");
        }

        private static string State()
        {
            var game = new char[Modifiers.Length];
            var legacy = new char[Modifiers.Length];
            var raw = new char[Modifiers.Length];
            var windows = new char[Modifiers.Length];
            var keyboard = Keyboard.current;
            for (int i = 0; i < Modifiers.Length; i++)
            {
                game[i] = ZInput.GetKey(Modifiers[i]) ? '1' : '0';
                legacy[i] = Input.GetKey(Modifiers[i]) ? '1' : '0';
                raw[i] = keyboard == null ? '?' : keyboard[RawModifiers[i]].isPressed ? '1' : '0';
                // Windows VK_LSHIFT through VK_RMENU are consecutive in this order.
                windows[i] = Application.platform == RuntimePlatform.WindowsPlayer
                    ? (GetAsyncKeyState(0xA0 + i) < 0 ? '1' : '0') : '?';
            }
            return $"focused={Application.isFocused} keyboard={keyboard?.deviceId} windows={new string(windows)} raw={new string(raw)} zinput={new string(game)} legacy={new string(legacy)}";
        }

        private void Event(string message)
        {
            writer.WriteLine(FormattableString.Invariant($"event={++events} frame={Time.frameCount} time={Time.realtimeSinceStartup:F3} ") + message);
            if (events < EventLimit) return;
            writer.WriteLine("Event limit reached. Restart the game to record a new report.");
            Dispose();
        }

        private void WriteSafely(Action write)
        {
            try { write(); }
            catch (IOException error) { StopAfterError(error); }
            catch (UnauthorizedAccessException error) { StopAfterError(error); }
        }

        private void StopAfterError(Exception error)
        {
            stopped = true;
            Jotunn.Logger.LogWarning($"Could not write Buildheim input report at {path}: {error.Message}. Recording stopped; rotation remains available. Check that BepInEx is writable, then restart to retry.");
            try { writer?.Dispose(); }
            catch (IOException) { /* The write failure has already been reported. */ }
            writer = null;
        }

        public void Dispose()
        {
            stopped = true;
            var closing = writer;
            writer = null;
            WriteSafely(() => closing?.Dispose());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StickyKeys { public uint Size; public uint Flags; }
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint action, uint size, ref StickyKeys value, uint flags);
    }
}
