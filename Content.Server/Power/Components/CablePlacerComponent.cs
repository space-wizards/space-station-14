using Robust.Shared.Prototypes;
using Content.Shared.Power;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class CablePlacerComponent : Component
    {
        [DataField("cablePrototypeID")]
        public EntProtoId? CablePrototypeId = "CableHV";

        [DataField("blockingWireType")]
        public CableType BlockingCableType = CableType.HighVoltage;
    }
}
