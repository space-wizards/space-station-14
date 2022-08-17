using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[NetSerializable, Serializable]
public readonly struct AdminGhostRoleGroupInfo
{
    public uint GroupIdentifier { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public GhostRoleGroupStatus Status { get; init; }
    public bool IsActive { get; init; }
    public bool CanModify { get; init; }
    public EntityUid[] Entities { get; init; }

}

[NetSerializable, Serializable]
public sealed class AdminGhostRolesEuiState : EuiStateBase
{
    public AdminGhostRoleGroupInfo[] AdminGhostRoleGroups { get; }

    public Dictionary<EntityUid, string> EntityNames { get; }

    public AdminGhostRolesEuiState(AdminGhostRoleGroupInfo[] groups, EntityManager entityManager)
    {
        AdminGhostRoleGroups = groups;

        EntityNames = new Dictionary<EntityUid, string>();
        foreach(var group in AdminGhostRoleGroups)
        {
            foreach (var entity in group.Entities)
            {
                EntityNames.Add(entity, entityManager.ToPrettyString(entity).Name ?? "Unknown");
            }
        }
    }
}
