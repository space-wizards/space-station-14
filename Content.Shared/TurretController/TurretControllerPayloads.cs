using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;

namespace Content.Shared.TurretController;

/// <summary>
/// A network payload that sets fire mode for a turret.
/// </summary>
public sealed partial class TurretControllerSetArmamentPayload : NetworkPayloadBase<TurretControllerSetArmamentPayload>
{
    [DataField]
    public int ArmamentState;
}

/// <summary>
/// A network payload that sets access exemptions for a turret.
/// </summary>
public sealed partial class TurretControllerSetAccessPayload : NetworkPayloadBase<TurretControllerSetAccessPayload>
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

/// <summary>
/// A network payload request to get the state of all available turrets.
/// </summary>
public sealed partial class TurretControllerRequestPayload : NetworkPayloadBase<TurretControllerRequestPayload>;
