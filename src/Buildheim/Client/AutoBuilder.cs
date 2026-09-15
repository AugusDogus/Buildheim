using UnityEngine;

namespace PlanBuild.Client
{
    internal sealed class AutoBuilder
    {
        private readonly AutoBuildQueue queue = new AutoBuildQueue();

        public HammerTarget Find(BlueprintProjection projection, Player player)
        {
            HammerTarget selected = null;
            queue.Next(projection.Pieces.Count, Time.time, index =>
            {
                var candidate = HammerTarget.FromPiece(projection, projection.Pieces[index], player);
                if (candidate == null || !candidate.RecipeKnown ||
                    !player.IsPieceAvailable(candidate.Piece) || !candidate.HasInventoryResources() ||
                    ZoneSystem.instance.GetGlobalKey(candidate.Piece.FreeBuildKey()) ||
                    !candidate.RequirementsMet) return false;
                selected = candidate;
                return true;
            });
            return selected;
        }

        public bool TryAttempt(Player player)
        {
            var tool = player.GetRightItem();
            return tool != null && (!tool.m_shared.m_useDurability || tool.m_durability > 0) &&
                player.HaveStamina(tool.m_shared.m_attack.m_attackStamina) &&
                Time.time - player.m_lastToolUseTime > player.m_placeDelay && queue.TryAttempt(Time.time);
        }

        public void Reset() => queue.Reset();
    }
}
