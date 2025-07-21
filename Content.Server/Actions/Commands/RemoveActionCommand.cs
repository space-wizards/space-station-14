using Content.Server.Administration;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Actions.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class RemoveActionCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override string Command => "rmaction";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString(Loc.GetString("cmd-rmaction-invalid-args")));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetUidNet) || !EntityManager.TryGetEntity(targetUidNet, out var targetEntity))
        {
            shell.WriteLine(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var targetActionUidNet) || !EntityManager.TryGetEntity(targetActionUidNet, out var targetActionEntity))
        {
            shell.WriteLine(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[1])));
            return;
        }

        if (!EntityManager.HasComponent<ActionsComponent>(targetEntity))
        {
            shell.WriteError(Loc.GetString("cmd-rmaction-actions-not-found"));
            return;
        }

        if (_actions.GetAction(targetActionEntity) is not { } ent)
        {
            shell.WriteError(Loc.GetString("cmd-rmaction-not-an-action"));
            return;
        }

        _actions.SetTemporary(ent.Owner, true);

        _actions.RemoveAction(ent.Owner);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<ActionsComponent>(args[0]),
                Loc.GetString("cmd-rmaction-player-completion"));
        }

        if (args.Length == 2)
        {
            if (!NetEntity.TryParse(args[0], out var targetUidNet) || !EntityManager.TryGetEntity(targetUidNet, out var targetEntity))
                return CompletionResult.Empty;

            if (!EntityManager.HasComponent<ActionsComponent>(targetEntity))
                return CompletionResult.Empty;

            var actions = _actions.GetActions(targetEntity.Value);

            var options = new List<CompletionOption>();
            foreach (var action in actions)
            {
                var hint = Loc.GetString("cmd-rmaction-action-info", ("action", action));
                options.Add(new CompletionOption(action.Owner.ToString(), hint));
            }

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-rmaction-action-completion"));
        }

        return CompletionResult.Empty;
    }
}
