using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class LinkBluespaceLocker : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

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

        if (!bool.TryParse(args[0], out var bidirectional))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var originUidNet) || !_entManager.TryGetEntity(originUidNet, out var originUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!NetEntity.TryParse(args[2], out var targetUidNet) || !_entManager.TryGetEntity(targetUidNet, out var targetUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.HasComponent<EntityStorageComponent>(originUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", originUid), ("componentName", nameof(EntityStorageComponent))));
            return;
        }

        if (!_entManager.HasComponent<EntityStorageComponent>(targetUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", targetUid), ("componentName", nameof(EntityStorageComponent))));
            return;
        }

        _entManager.EnsureComponent<BluespaceLockerComponent>(originUid.Value, out var originBluespaceComponent);
        originBluespaceComponent.BluespaceLinks.Add(targetUid.Value);
        _entManager.EnsureComponent<BluespaceLockerComponent>(targetUid.Value, out var targetBluespaceComponent);
        if (bidirectional)
        {
            targetBluespaceComponent.BluespaceLinks.Add(originUid.Value);
        }
        else if (targetBluespaceComponent.BluespaceLinks.Count == 0)
        {
            targetBluespaceComponent.BehaviorProperties.TransportSentient = false;
            targetBluespaceComponent.BehaviorProperties.TransportEntities = false;
            targetBluespaceComponent.BehaviorProperties.TransportGas = false;
        }
    }
}
