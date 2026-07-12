using Content.Shared.DeviceNetwork;

namespace Content.Shared.Turrets;

/// <summary>
/// A wrapper for <see cref="DeployableTurretState"/>.
/// </summary>
public sealed partial class TurretStatePayload : NetworkPayloadBase<TurretStatePayload>
{
    [DataField]
    public DeployableTurretState State;
}
