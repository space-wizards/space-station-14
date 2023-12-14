using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Stores the ninja's objectives on the mind so if they die the rest of the greentext persists.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [DataField("spiderChargeTarget")]
    public EntityUid? SpiderChargeTarget;
}
