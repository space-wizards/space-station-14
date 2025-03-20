using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class VaporComponent : Component
    {
        public const string SolutionName = "vapor";

        [DataField]
        public FixedPoint2 TransferAmount = FixedPoint2.New(0.15);

        [DataField]
        public bool Active;
    }
}
