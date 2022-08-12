using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Components;

/// <summary>
/// This is used for...
/// </summary>
public abstract class SharedTargetingConsoleComponent : Component
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
public enum TargetingConsoleUiKey : byte
{
    Key,
}
