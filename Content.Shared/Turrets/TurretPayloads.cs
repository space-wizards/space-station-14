using Content.Shared.DeviceNetwork;

namespace Content.Shared.Turrets;

/// <summary>
/// A wrapper for <see cref="DeployableTurretState"/>.
/// </summary>
public partial record struct TurretStatePayload : INetworkPayload
{
    [DataField]
    public DeployableTurretState State;
}
