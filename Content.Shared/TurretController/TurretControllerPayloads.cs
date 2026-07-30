using Content.Shared.Access;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Prototypes;

namespace Content.Shared.TurretController;

/// <summary>
/// A network payload that sets fire mode for a turret.
/// </summary>
public partial record struct TurretControllerSetArmamentPayload : INetworkPayload
{
    [DataField]
    public int ArmamentState;
}

/// <summary>
/// A network payload that sets access exemptions for a turret.
/// </summary>
public partial record struct TurretControllerSetAccessPayload : INetworkPayload
{
    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessExemptions = new();
}

/// <summary>
/// A network payload request to get the state of all available turrets.
/// </summary>
public partial record struct TurretControllerRequestPayload : INetworkPayload;
