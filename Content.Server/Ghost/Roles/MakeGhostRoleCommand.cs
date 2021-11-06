using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Fun)]
    public class MakeGhostRoleCommand : IConsoleCommand
    {
        public string Command => "makeghostrole";
        public string Description => "Turns an entity into a ghost role.";
        public string Help => $"Usage: {Command} <entity uid> <name> <description> [<rules>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine($"{args[0]} is not a valid entity uid.");
                return;
            }

            if (!entityManager.TryGetEntity(uid, out var entity))
            {
                shell.WriteLine($"No entity found with uid {uid}");
                return;
            }

            if (entity.TryGetComponent(out MindComponent? mind) &&
                mind.HasMind)
            {
                shell.WriteLine($"Entity {entity.Name} with id {uid} already has a mind.");
                return;
            }

            var name = args[1];
            var description = args[2];
            var rules = args.Length >= 4 ? args[3] : Loc.GetString("ghost-role-component-default-rules");

            if (entity.EnsureComponent(out GhostTakeoverAvailableComponent takeOver))
            {
                shell.WriteLine($"Entity {entity.Name} with id {uid} already has a {nameof(GhostTakeoverAvailableComponent)}");
                return;
            }

            takeOver.RoleName = name;
            takeOver.RoleDescription = description;
            takeOver.RoleRules = rules;

            shell.WriteLine($"Made entity {entity.Name} a ghost role.");
        }
    }
}
