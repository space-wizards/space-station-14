using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Client.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class HyposprayComponent : SharedHyposprayComponent
    {
        [ViewVariables]
        public FixedPoint2 CurrentVolume;
        [ViewVariables]
        public FixedPoint2 TotalVolume;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;
    }
}
