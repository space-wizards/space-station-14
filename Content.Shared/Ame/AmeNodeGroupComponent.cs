using Robust.Shared.GameStates;

namespace Content.Shared.Ame;

/// <summary>
/// Node group class for handling the Antimatter Engine's console and parts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmeNodeGroupComponent : Component
{
    /// <summary>
    /// The AME controller which is currently in control of this node group.
    /// This could be tracked a few different ways, but this is most convenient,
    /// since any part connected to the node group can easily find the master.
    /// </summary>
    [ViewVariables]
    public EntityUid? MasterController;

    /// <summary>
    /// The set of AME shielding units that currently count as cores for the AME.
    /// </summary>
    public readonly List<EntityUid> Cores = new();
}
