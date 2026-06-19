using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.TurretController;

[Serializable, NetSerializable]
public sealed partial class TurretControllerSetArmamentPayload : NetworkPayload
{
    [DataField]
    public int ArmamentState;
}

[Serializable, NetSerializable]
public sealed partial class TurretControllerSetAccessPayload : NetworkPayload
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

[Serializable, NetSerializable]
public sealed partial class TurretControllerRequestPayload : NetworkPayload;
