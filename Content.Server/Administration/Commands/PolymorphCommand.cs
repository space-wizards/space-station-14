using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Content.Shared.Polymorph;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class PolymorphCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public string Command => "polymorph";

    public string Description => Loc.GetString("polymorph-command-description");

    public string Help => Loc.GetString("polymorph-command-help-text");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !_entManager.TryGetEntity(entityUidNet, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_protoManager.TryIndex<PolymorphPrototype>(args[1], out var polyproto))
        {
            shell.WriteError(Loc.GetString("polymorph-not-valid-prototype-error"));
            return;
        }

        var polySystem = _entManager.EntitySysManager.GetEntitySystem<PolymorphSystem>();

        _entManager.EnsureComponent<PolymorphableComponent>(entityUid);
        polySystem.PolymorphEntity(entityUid, polyproto);
    }
}
