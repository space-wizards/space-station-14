using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// Interact with to use ship guns.
/// </summary>
[NetworkedComponent]
public abstract class SharedTurretConsoleComponent : Component
{
    // Only one because tracking more than one mouse sounds fucking awful.
    /// <summary>
    /// Who's using the turret console at the moment
    /// </summary>
    [ViewVariables]
    public GunnerComponent? SubscribedGunner = null;

    public GimbalGunTargetComponent? Target;
}

[Serializable, NetSerializable]
public enum TurretConsoleUiKey : byte
{
    Key,
}
