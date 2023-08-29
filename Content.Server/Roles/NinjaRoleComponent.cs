using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Stores the ninja's objectives on the mind so if they die the rest of the greentext persists.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// Number of doors that have been doorjacked, used for objective
    /// </summary>
    [ViewVariables]
    public int DoorsJacked = 0;

    /// <summary>
    /// Research nodes that have been downloaded, used for objective
    /// </summary>
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
