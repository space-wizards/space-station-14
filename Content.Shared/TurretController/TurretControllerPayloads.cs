using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.TurretController;

[Serializable, NetSerializable]
public sealed partial class TurretControllerSetArmamentPayload : NetworkPayloadBase<TurretControllerSetArmamentPayload>
{
    [DataField]
    public int ArmamentState;
}

[Serializable, NetSerializable]
public sealed partial class TurretControllerSetAccessPayload : NetworkPayloadBase<TurretControllerSetAccessPayload>
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

[Serializable, NetSerializable]
public sealed partial class TurretControllerRequestPayload : NetworkPayloadBase<TurretControllerRequestPayload>;
