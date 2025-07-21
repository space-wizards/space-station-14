using System.Linq;
using Content.Server.Administration;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Actions.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddActionCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "addaction";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString(Loc.GetString("cmd-addaction-invalid-args")));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetUidNet) || !EntityManager.TryGetEntity(targetUidNet, out var targetEntity))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityManager.HasComponent<ActionsComponent>(targetEntity))
        {
            shell.WriteError(Loc.GetString("cmd-addaction-actions-not-found"));
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(args[1], out var proto) ||
            !proto.HasComponent<ActionComponent>())
        {
            shell.WriteError(Loc.GetString("cmd-addaction-action-not-found", ("action", args[1])));
            return;
        }

        if (_actions.AddAction(targetEntity.Value, args[1]) == null)
        {
            shell.WriteError(Loc.GetString("cmd-addaction-adding-failed"));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<ActionsComponent>(args[0]),
                Loc.GetString("cmd-addaction-player-completion"));
        }

        if (args.Length != 2)
            return CompletionResult.Empty;

        var actionPrototypes = _prototypeManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.HasComponent<ActionComponent>())
            .Select(p => p.ID)
            .Order();

        return CompletionResult.FromHintOptions(
            actionPrototypes,
            Loc.GetString("cmd-addaction-action-completion"));
    }
}
