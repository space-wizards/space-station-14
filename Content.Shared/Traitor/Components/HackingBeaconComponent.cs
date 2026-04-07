using Robust.Shared.GameStates;

namespace Content.Shared.Traitor.Components;

/// <summary>
/// Marks that this item is a hacking beacon that will hack infrastructure it is planted onto.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HackingBeaconComponent : Component
{
    /// <summary>
    /// How long has this beacon been planted, if at all?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimePlanted = TimeSpan.Zero;

    /// <summary>
    /// What time should we increment TimePlanted next?
    /// </summary>

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// How often should we increment TimePlanted?
    /// </summary>

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Has this completed a hack on what it's currently planted on?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HackCompleted = false;
}
