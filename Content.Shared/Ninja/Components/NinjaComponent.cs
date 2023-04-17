using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component placed on a mob to make it a space ninja, able to use suit and glove powers.
/// Contains ids of all ninja equipment.
/// </summary>
// TODO: Contains objective related stuff, might want to move it out somehow
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNinjaSystem))]
public sealed partial class NinjaComponent : Component
{
    /// <summary>
    /// Grid entity of the station the ninja was spawned around. Set if spawned naturally by the event.
    /// </summary>
    public EntityUid? StationGrid;

    /// <summary>
    /// Currently worn suit
    /// </summary>
    [ViewVariables]
    public EntityUid? Suit = null;

    /// <summary>
    /// Currently worn gloves
    /// </summary>
    [ViewVariables]
    public EntityUid? Gloves = null;

    /// <summary>
    /// Bound katana, set once picked up and never removed
    /// </summary>
    [ViewVariables]
    public EntityUid? Katana = null;

    /// <summary>
    /// Number of doors that have been doorjacked, used for objective
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int DoorsJacked = 0;

    /// <summary>
    /// Research nodes that have been downloaded, used for objective
    /// </summary>
    // TODO: client doesn't need to know what nodes are downloaded, just how many
    [ViewVariables, AutoNetworkedField]
    public HashSet<string> DownloadedNodes = new();

    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? SpiderChargeTarget = null;

    /// <summary>
    /// Whether the spider charge has been detonated on the target, used for objective
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool SpiderChargeDetonated;

    /// <summary>
    /// Whether the comms console has been hacked, used for objective
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CalledInThreat;
}
