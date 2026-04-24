using Robust.Shared.GameStates;

namespace Content.Shared.Traitor.Components;

/// <summary>
/// Marks that an entity is an active hacking beacon. Used for not having to query inactive beacons.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveHackingBeaconComponent : Component
{
    /// <summary>
    /// When was this beacon planted? If it is currently not planted, returns zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimePlanted = TimeSpan.Zero;

    /// <summary>
    /// Has this completed a hack on what it's currently planted on?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HackCompleted = false;
}
