using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles;

[AdminCommand(AdminFlags.Admin)]
public sealed class MakeGhostRoleCommand : LocalizedEntityCommands
{
    public override string Command => "makeghostrole";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 4)
        {
            shell.WriteLine($"Invalid amount of arguments.\n{Help}");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !EntityManager.TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine($"{args[0]} is not a valid entity uid.");
            return;
        }

        if (!EntityManager.TryGetComponent(uid, out MetaDataComponent? metaData))
        {
            shell.WriteLine($"No entity found with uid {uid}");
            return;
        }

        if (EntityManager.TryGetComponent(uid, out MindContainerComponent? mind) && mind.HasMind)
        {
            shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a mind.");
            return;
        }

        var name = args[1];
        var description = args[2];
        var rules = args.Length >= 4 ? args[3] : Loc.GetString("ghost-role-component-default-rules");

        if (EntityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostRoleComponent)}");
            return;
        }

        if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
        {
            shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostTakeoverAvailableComponent)}");
            return;
        }

        ghostRole = EntityManager.AddComponent<GhostRoleComponent>(uid.Value);
        EntityManager.AddComponent<GhostTakeoverAvailableComponent>(uid.Value);
        ghostRole.RoleName = name;
        ghostRole.RoleDescription = description;
        ghostRole.RoleRules = rules;

        shell.WriteLine($"Made entity {metaData.EntityName} a ghost role.");
    }
}
