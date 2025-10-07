﻿using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Administration;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeGhostRoleCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "makeghostrole";

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-invalid-args"));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-invalid-entity-uid", ("entity", args[0])));
                return;
            }

            if (!_entManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-entity-not-found", ("entity", uid)));
                return;
            }

            if (_entManager.TryGetComponent(uid, out MindContainerComponent? mind) &&
                mind.HasMind)
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-entity-has-mind", ("entity", metaData.EntityName), ("uid", uid)));
                return;
            }

            var name = args[1];
            var description = args[2];
            var rules = args.Length >= 4 ? args[3] : Loc.GetString("ghost-role-component-default-rules");

            if (_entManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-entity-has-ghost-role", ("entity", metaData.EntityName), ("uid", uid)));
                return;
            }

            if (_entManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                shell.WriteError(Loc.GetString($"cmd-{Command}-entity-has-ghost-takeover", ("entity", metaData.EntityName), ("uid", uid)));
                return;
            }

            ghostRole = _entManager.AddComponent<GhostRoleComponent>(uid.Value);
            _entManager.AddComponent<GhostTakeoverAvailableComponent>(uid.Value);
            ghostRole.RoleName = name;
            ghostRole.RoleDescription = description;
            ghostRole.RoleRules = rules;

            shell.WriteLine(Loc.GetString($"cmd-{Command}-made-ghost-role", ("entity", metaData.EntityName)));
        }
    }
}
