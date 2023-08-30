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
    [DataField("doorsJacked")]
    public int DoorsJacked;

    /// <summary>
    /// Research nodes that have been downloaded, used for objective
    /// </summary>
    [DataField("downloadedNodes")]
    public HashSet<string> DownloadedNodes = new();

    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [DataField("spiderChargeTarget")]
    public EntityUid? SpiderChargeTarget;

    /// <summary>
    /// Whether the spider charge has been detonated on the target, used for objective
    /// </summary>
    [DataField("spiderChargeDetonated")]
    public bool SpiderChargeDetonated;

    /// <summary>
    /// Whether the comms console has been hacked, used for objective
    /// </summary>
    [DataField("calledInThreat")]
    public bool CalledInThreat;
}
