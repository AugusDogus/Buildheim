namespace PlanBuild.Client
{
    // A hologram can be inspected even when it has no usable hammer build target.
    internal abstract class BlueprintSelection
    {
        public BlueprintProjection.ProjectedPiece Planned { get; }
        private BlueprintSelection(BlueprintProjection.ProjectedPiece planned) { Planned = planned; }

        internal sealed class Hammer : BlueprintSelection
        {
            public HammerTarget Target { get; }
            public Hammer(HammerTarget target) : base(target.Planned) { Target = target; }
        }

        internal sealed class Guide : BlueprintSelection
        {
            public string Reason { get; }
            public Guide(BlueprintProjection.ProjectedPiece planned, string reason) : base(planned) { Reason = reason; }
        }
    }
}
