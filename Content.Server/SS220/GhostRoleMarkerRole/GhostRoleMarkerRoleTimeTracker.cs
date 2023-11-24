// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Ghost.Roles;
using Content.Shared.Roles;

namespace Content.Server.SS220.GhostRoleMarkerRole;

public sealed class GhostRoleMarkerRoleTimeTracker : EntitySystem
{
    private const string UnknownRoleName = "game-ticker-unknown-role";
    private const string GhostRoleTracker = "JobGhostRole";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleMarkerRoleComponent, MindGetAllRolesEvent>(OnMindGetAllRoles);
    }

    private void OnMindGetAllRoles(EntityUid uid, GhostRoleMarkerRoleComponent component, ref MindGetAllRolesEvent args)
    {
        string name = component.Name == null ? UnknownRoleName : component.Name;
        args.Roles.Add(new RoleInfo(component, name, false, GhostRoleTracker));
    }
}
