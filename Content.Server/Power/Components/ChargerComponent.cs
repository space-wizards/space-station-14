using Content.Shared.Power;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class ChargerComponent : Component
    {
        [ViewVariables]
        public CellChargerStatus Status;

        [DataField("chargeRate")]
        public int ChargeRate = 20;

        [DataField("slotId", required: true)]
        public string SlotId = string.Empty;
    }
}
