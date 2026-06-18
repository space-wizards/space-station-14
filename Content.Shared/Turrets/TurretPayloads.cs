using Content.Shared.DeviceNetwork;

namespace Content.Shared.Turrets;

public sealed partial class TurretStatePayload : NetworkPayload
{
    [DataField]
    public DeployableTurretState State;
}
