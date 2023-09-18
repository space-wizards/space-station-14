using Content.Server.Dragon;
using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
/// Role used to keep track of space dragons for greentext and tracking total rifts charged.
/// </summary>
[RegisterComponent, Access(typeof(DragonSystem))]
public sealed partial class DragonRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// Number of carp rifts that are fully charged.
    /// </summary>
    [DataField]
    public int RiftsCharged;
}
