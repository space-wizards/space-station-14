using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class LinkBluespaceLocker : IConsoleCommand
{
    public string Command => "linkbluespacelocker";
    public string Description => "Links an entity, the target, to another as a bluespace locker target.";
    public string Help => "Usage: linkbluespacelocker <two-way link> <origin storage uid> <target storage uid>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!Boolean.TryParse(args[0], out var bidirectional))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var originUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityUid.TryParse(args[2], out var targetUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();

        if (!entityManager.TryGetComponent<EntityStorageComponent>(originUid, out var originComponent))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", originUid), ("componentName", nameof(EntityStorageComponent))));
            return;
        }

        if (!entityManager.TryGetComponent<EntityStorageComponent>(targetUid, out var targetComponent))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", targetUid), ("componentName", nameof(EntityStorageComponent))));
            return;
        }

        entityManager.EnsureComponent<BluespaceLockerComponent>(originUid, out var originBluespaceComponent);
        originBluespaceComponent.BluespaceLinks.Add(targetComponent);
        entityManager.EnsureComponent<BluespaceLockerComponent>(targetUid, out var targetBluespaceComponent);
        if (bidirectional)
        {
            targetBluespaceComponent.BluespaceLinks.Add(originComponent);
        }
        else if (targetBluespaceComponent.BluespaceLinks.Count == 0)
        {
            targetBluespaceComponent.AllowSentient = false;
            targetBluespaceComponent.TransportEntities = false;
            targetBluespaceComponent.TransportGas = false;
        }
    }
}
