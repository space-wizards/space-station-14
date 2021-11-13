using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cleanable
{
    [RegisterComponent]
    public class CleanableComponent : Component
    {
        public override string Name => "Cleanable";

        [DataField("cleanAmount")]
        private FixedPoint2 _cleanAmount = FixedPoint2.Zero;
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 CleanAmount => _cleanAmount;
    }
}
