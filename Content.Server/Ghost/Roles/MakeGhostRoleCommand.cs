using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeGhostRoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "makeghostrole";
        public string Description => "Turns an entity into a ghost role.";
        public string Help => $"Usage: {Command} <entity uid> <name> <description> [rules] [allowMovement = false] [allowSpeech = true]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 6)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine($"{args[0]} is not a valid entity uid.");
                return;
            }

            if (!_entManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                shell.WriteLine($"No entity found with uid {uid}");
                return;
            }

            if (_entManager.TryGetComponent(uid, out MindContainerComponent? mind) &&
                mind.HasMind)
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a mind.");
                return;
            }

            var name = args[1];
            var description = args[2];
            var rules = (args.Length >= 4 && args[3] != String.Empty) ? args[3] : Loc.GetString("ghost-role-component-default-rules");
            var allowMovement = false;
            var allowSpeech = true;
            if(args.Length >= 5 && !bool.TryParse(args[4], out allowMovement))
            {
                shell.WriteLine($"Optional argument 5 \"allowMovement\" must be \"true\" or \"false\".\n{Help}");
                return;
            }
            if (args.Length >= 6 && !bool.TryParse(args[5], out allowSpeech))
            {
                shell.WriteLine($"Optional argument 6 \"allowSpeech\" must be \"true\" or \"false\".\n{Help}");
                return;
            }

            if (_entManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostRoleComponent)}");
                return;
            }

            if (_entManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostTakeoverAvailableComponent)}");
                return;
            }

            _entManager.AddComponent<GhostRoleComponent>(uid.Value);
            _entManager.AddComponent<GhostTakeoverAvailableComponent>(uid.Value);
            _entManager.TrySystem(out GhostRoleSystem? ghostRoleSystem);
            if (ghostRoleSystem != null)
            {
                ghostRoleSystem.SetInformation(uid.Value, name, description, rules, allowSpeech, allowMovement);
            }

            shell.WriteLine($"Made entity {metaData.EntityName} a ghost role.");
        }
    }
}
