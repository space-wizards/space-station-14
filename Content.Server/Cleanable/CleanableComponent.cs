using Content.Shared.FixedPoint;

namespace Content.Server.Cleanable
{
    [RegisterComponent]
    public sealed class CleanableComponent : Component
    {
        [DataField("cleanAmount")]
        private FixedPoint2 _cleanAmount = FixedPoint2.Zero;
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 CleanAmount => _cleanAmount;
    }
}
