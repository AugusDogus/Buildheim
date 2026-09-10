namespace PlanBuild.Client
{
    internal static class PlacementFeedback
    {
        public static string Describe(Player.PlacementStatus status) => status switch
        {
            Player.PlacementStatus.Valid => "Ready to build.",
            Player.PlacementStatus.NoRayHits => "No reachable surface touches this piece. Move closer or build its foundation first.",
            Player.PlacementStatus.BlockedbyPlayer => "A player is standing in this piece. Step aside to build.",
            Player.PlacementStatus.NoBuildZone => "Building is forbidden at this location.",
            Player.PlacementStatus.PrivateZone => "A ward prevents building here. You need access.",
            Player.PlacementStatus.MoreSpace => "This piece needs more space from nearby objects.",
            Player.PlacementStatus.NoTeleportArea => "This piece requires a teleport area.",
            Player.PlacementStatus.ExtensionMissingStation => "Move within range of the crafting station this upgrade belongs to.",
            Player.PlacementStatus.WrongBiome => "This piece cannot be built in this biome.",
            Player.PlacementStatus.NeedCultivated => "Cultivate the ground under this piece first.",
            Player.PlacementStatus.NeedDirt => "This piece needs suitable soil beneath it.",
            Player.PlacementStatus.NotInDungeon => "This piece cannot be built inside a dungeon.",
            Player.PlacementStatus.DeepSnow => "Clear the deep snow under this piece first.",
            Player.PlacementStatus.NoSnow => "This piece requires deep snow.",
            _ => "Valheim rejected this position. Check overlap, support and surface restrictions, or adjust the hologram."
        };
    }
}
