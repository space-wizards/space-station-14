using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed class ChargerComponent : Component
    {
        [ViewVariables]
        public CellChargerStatus Status;

        [ViewVariables]
        [DataField("chargeRate")]
        public int ChargeRate = 20;

        [ViewVariables]
        [DataField("slotId", required: true)]
        public string SlotId = string.Empty;
    }
}
