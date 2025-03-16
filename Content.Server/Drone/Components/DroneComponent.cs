using Content.Shared.Whitelist;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server.Drone.Components
{
    [RegisterComponent]
    [AutoGenerateComponentPause]
    public sealed partial class DroneComponent : Component
    {
        public float InteractionBlockRange = 1.5f; /// imp. original value was 2.15, changed because it was annoying. this also does not actually block interactions anymore.

        // imp. delay before posting another proximity alert
        public TimeSpan ProximityDelay = TimeSpan.FromMilliseconds(2000);

        [AutoPausedField]
        public TimeSpan NextProximityAlert = new();

        public EntityUid NearestEnt = default!;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Whitelist;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Blacklist;

        [DataField]
        public ProtoId<AlertPrototype> BatteryAlert = "DroneBattery";

        [DataField]
        public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

        public short LastChargePercent;
    }
}
