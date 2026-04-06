using Robust.Shared.GameStates;

namespace Content.Shared.Traitor.Components;

/// <summary>
/// Marks that this structure can be hacked by a traitor via a hacking beacon.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeaconHackableComponent : Component
{
    /// <summary>
    /// Has this been hacked?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Hacked = false;

    /// <summary>
    /// Can this be hacked multiple times?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Repeatable = false;
}
