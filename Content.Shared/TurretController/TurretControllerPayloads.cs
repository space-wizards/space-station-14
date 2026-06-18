using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;

namespace Content.Shared.TurretController;

public sealed partial class TurretControllerSetArmamentPayload : NetworkPayload
{
    [DataField]
    public int ArmamentState;
}

public sealed partial class TurretControllerSetAccessPayload : NetworkPayload
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

public sealed partial class TurretControllerRequestPayload : NetworkPayload;
