using Robust.Shared.GameStates;

namespace Content.Shared.Traitor.Components;

/// <summary>
/// Marks that this item is a hacking beacon that will hack infrastructure it is planted onto.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HackingBeaconComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan TimePlanted = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool HackCompleted = false;
}