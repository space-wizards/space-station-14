using Content.Shared.FixedPoint;
using Content.Shared.Vapor;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class VaporComponent : Component
    {
        public const string SolutionName = "vapor";

        [ViewVariables]
        [DataField("transferAmount")]
        public FixedPoint2 TransferAmount = FixedPoint2.New(0.5);

        public float ReactTimer;
        public bool Active;
    }
}
