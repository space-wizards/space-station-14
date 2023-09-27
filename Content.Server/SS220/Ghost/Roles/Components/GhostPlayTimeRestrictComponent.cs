using Content.Server.Ghost.Roles;

namespace Content.Server.SS220.Ghost.Roles.Components
{
    /// <summary>
    /// Require play time for player to take ghost role.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed partial class GhostPlayTimeRestrictComponent : Component
    {
    }
}
