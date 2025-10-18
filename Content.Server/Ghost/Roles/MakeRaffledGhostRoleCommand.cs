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
    public sealed class MakeRaffledGhostRoleCommand : LocalizedCommands
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "makeghostroleraffled";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is < 4 or > 7)
            {
                shell.WriteError(Loc.GetString("shell-need-between-arguments", ("lower", 4), ("upper", 7)));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteError(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
                return;
            }

            if (!_entManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                shell.WriteError(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", uid)));
                return;
            }

            if (_entManager.TryGetComponent(uid, out MindContainerComponent? mind) &&
                mind.HasMind)
            {
                shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-entity-has-mind", ("entity", metaData.EntityName), ("uid", uid)));
                return;
            }

            if (_entManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
            {
                shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-entity-has-ghost-role", ("entity", metaData.EntityName), ("uid", uid)));
                return;
            }

            if (_entManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-entity-has-ghost-takeover", ("entity", metaData.EntityName), ("uid", uid)));
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

                    shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-invalid-raffle-settings-prototype", ("prototype", args[3]), ("validProtos", validProtos)));
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
                    shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-invalid-raffle-settings"));
                    return;
                }

                if (initial > max)
                {
                    shell.WriteError(Loc.GetString("cmd-makeghostroleraffled-invalid-raffle-settings"));
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

            shell.WriteLine(Loc.GetString("cmd-makeghostroleraffled-made-raffled-ghost-role", ("entity", metaData.EntityName)));
        }
    }
}
