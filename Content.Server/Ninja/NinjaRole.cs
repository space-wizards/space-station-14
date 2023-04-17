using Content.Server.Traitor;
using Content.Shared.Roles;

namespace Content.Server.Ninja;

/// <summary>
/// Stores the ninja's objectives in the mind so if they die the rest of the greentext persists.
/// </summary>
public sealed class NinjaRole : TraitorRole
{
    public NinjaRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }

    /// <summary>
    /// Number of doors that have been doorjacked, used for objective
    /// </summary>
    [ViewVariables]
    public int DoorsJacked = 0;

    /// <summary>
    /// Research nodes that have been downloaded, used for objective
    /// </summary>
    // TODO: client doesn't need to know what nodes are downloaded, just how many
    [ViewVariables]
    public HashSet<string> DownloadedNodes = new();

    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [ViewVariables]
    public EntityUid? SpiderChargeTarget = null;

    /// <summary>
    /// Whether the spider charge has been detonated on the target, used for objective
    /// </summary>
    [ViewVariables]
    public bool SpiderChargeDetonated;

    /// <summary>
    /// Whether the comms console has been hacked, used for objective
    /// </summary>
    [ViewVariables]
    public bool CalledInThreat;
}
