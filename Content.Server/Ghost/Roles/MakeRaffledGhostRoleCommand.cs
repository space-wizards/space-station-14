using System.Linq;
using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Raffles;
using Content.Shared.Administration;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeRaffledGhostRoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "makeghostroleraffled";
        public string Description => "Turns an entity into a raffled ghost role.";
        public string Help => $"Usage: {Command} <entity uid> <name> <description> (<settings prototype> | <initial duration> <extend by> <max duration>) [<rules>]\n" +
                              $"Durations are in seconds.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 4 or > 7)
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

            var name = args[1];
            var description = args[2];

            // if the rules are specified then use those, otherwise use the default
            var rules = args.Length switch
            {
                5 => args[4],
                7 => args[6],
                _ => Loc.GetString("ghost-role-component-default-rules"),
            };

            // is it an invocation with a prototype ID and optional rules?
            var isProto = args.Length is 4 or 5;
            GhostRoleRaffleSettings settings;

            if (isProto)
            {
                if (!_protoManager.TryIndex<GhostRoleRaffleSettingsPrototype>(args[3], out var proto))
                {
                    var validProtos = string.Join(", ",
                        _protoManager.EnumeratePrototypes<GhostRoleRaffleSettingsPrototype>().Select(p => p.ID)
                    );

                    shell.WriteLine($"{args[3]} is not a valid raffle settings prototype. Valid options: {validProtos}");
                    return;
                }

                settings = proto.Settings;
            }
            else
            {
                if (!uint.TryParse(args[3], out var initial)
                    || !uint.TryParse(args[4], out var extends)
                    || !uint.TryParse(args[5], out var max)
                    || initial == 0 || max == 0)
                {
                    shell.WriteLine($"The raffle initial/extends/max settings must be positive numbers.");
                    return;
                }

                if (initial > max)
                {
                    shell.WriteLine("The initial duration must be smaller than or equal to the maximum duration.");
                    return;
                }

                settings = new GhostRoleRaffleSettings()
                {
                    InitialDuration = initial,
                    JoinExtendsDurationBy = extends,
                    MaxDuration = max
                };
            }

            ghostRole = _entManager.AddComponent<GhostRoleComponent>(uid.Value);
            _entManager.AddComponent<GhostTakeoverAvailableComponent>(uid.Value);
            ghostRole.RoleName = name;
            ghostRole.RoleDescription = description;
            ghostRole.RoleRules = rules;
            ghostRole.RaffleConfig = new GhostRoleRaffleConfig(settings);

            shell.WriteLine($"Made entity {metaData.EntityName} a raffled ghost role.");
        }
    }
}
