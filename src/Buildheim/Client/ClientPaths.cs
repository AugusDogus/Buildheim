using System;
using System.IO;

namespace PlanBuild.Client
{
    internal static class ClientPaths
    {
        // Reuse an existing data directory so upgrading never moves or resurrects saved plans.
        public static string DataDirectory(string configRoot)
        {
            string current = Path.Combine(configRoot, "Buildheim");
            string legacy = Path.Combine(configRoot, "PlanBuild");
            return Directory.Exists(current) || !Directory.Exists(legacy) ? current : legacy;
        }

        public static bool TryMigrateConfig(string configRoot, string destination, out bool copied, out string error)
        {
            copied = false;
            error = string.Empty;
            string legacy = Path.Combine(configRoot, "marcopogo.PlanBuild.cfg");
            try
            {
                if (File.Exists(destination) || !File.Exists(legacy)) return true;
                File.Copy(legacy, destination, false);
                copied = true;
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                error = $"Buildheim could not copy settings from {legacy} to {destination}: {ex.Message} " +
                    "The original settings and blueprints are preserved. Check permissions before restarting.";
                return false;
            }
        }
    }
}
