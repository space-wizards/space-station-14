using Content.Shared.DeviceNetwork;

namespace Content.Shared.Turrets;

public sealed partial class TurretStatePayload : NetworkPayloadBase<TurretStatePayload>
{
    [DataField]
    public DeployableTurretState State;
}
