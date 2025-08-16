using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.FeedbackSystem;

[AdminCommand(AdminFlags.Server)]
public sealed class FeedbackPopupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedFeedbackSystem _feedback = default!;

    public override string Command => Loc.GetString("feedbackpopup-command-name");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-wrong-arguments"));
            return;
        }

        if (!int.TryParse(args[0], out var entityUidInt))
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-invalid-uid"));
            return;
        }

        var netEnt = new NetEntity(entityUidInt);

        if (!EntityManager.TryGetEntity(netEnt, out var target))
        {
            shell.WriteLine(Loc.GetString("feedbackpopup-command-error-entity-not-found"));
            return;
        }

        if (!_proto.HasIndex<FeedbackPopupPrototype>(args[1]))
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-invalid-proto"));
            return;
        }

        if (!_feedback.SendPopups(target, [args[1]]))
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-popup-send-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("feedbackpopup-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("feedbackpopup-command-hint-playerUid"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(_feedback.FeedbackPopupProtoIds.Select(x => (string) x), Loc.GetString("feedbackpopup-command-hint-protoId"));
        }
        return CompletionResult.Empty;
    }
}
