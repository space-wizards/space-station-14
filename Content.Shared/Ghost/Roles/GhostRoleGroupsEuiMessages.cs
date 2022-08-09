using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[NetSerializable, Serializable]
public struct AdminGhostRoleGroupInfo
{
    public uint GroupIdentifier { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public EntityUid[] Entities { get; set; }
}

[NetSerializable, Serializable]
public sealed class AdminGhostRolesEuiState : EuiStateBase
{
    public AdminGhostRoleGroupInfo[] AdminGhostRoleGroups { get; }

    public AdminGhostRolesEuiState(AdminGhostRoleGroupInfo[] groups)
    {
        AdminGhostRoleGroups = groups;
    }
}
