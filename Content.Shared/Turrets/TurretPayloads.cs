using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Turrets;

[Serializable, NetSerializable]
public sealed partial class TurretStatePayload : NetworkPayload
{
    [DataField]
    public DeployableTurretState State;
}
