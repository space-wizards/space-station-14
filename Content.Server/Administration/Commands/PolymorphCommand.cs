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
    public string Command => "polymorph";

    public string Description => Loc.GetString("polymorph-command-description");

    public string Help => Loc.GetString("polymorph-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex<PolymorphPrototype>(args[1], out var polyproto))
        {
            shell.WriteError(Loc.GetString("polymorph-not-valid-prototype-error"));
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var polySystem = entityManager.EntitySysManager.GetEntitySystem<PolymorphableSystem>();

        entityManager.EnsureComponent<PolymorphableComponent>(entityUid);
        polySystem.PolymorphEntity(entityUid, polyproto);
    }
}
