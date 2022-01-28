using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Vapor;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    internal sealed class VaporComponent : SharedVaporComponent
    {
        [ViewVariables]
        [DataField("transferAmount")]
        internal FixedPoint2 TransferAmount = FixedPoint2.New(0.5);

        internal bool Reached;
        internal float ReactTimer;
        internal float Timer;
        internal EntityCoordinates Target;
        internal bool Active;
        internal float AliveTime;
    }
}
