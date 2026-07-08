using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;

namespace Content.Shared.TurretController;

public sealed partial class TurretControllerSetArmamentPayload : NetworkPayloadBase<TurretControllerSetArmamentPayload>
{
    [DataField]
    public int ArmamentState;
}

public sealed partial class TurretControllerSetAccessPayload : NetworkPayloadBase<TurretControllerSetAccessPayload>
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

public sealed partial class TurretControllerRequestPayload : NetworkPayloadBase<TurretControllerRequestPayload>;
