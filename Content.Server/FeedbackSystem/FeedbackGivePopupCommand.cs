using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.FeedbackSystem;

/// <summary>
/// Give a session a specific feedback pop-up.
/// </summary>
[AdminCommand(AdminFlags.Server)]
public sealed class FeedbackGivePopupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedFeedbackSystem _feedback = default!;

    public override string Command => Loc.GetString("feedbackpopup-give-command-name");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var entityUidInt))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var netEnt = new NetEntity(entityUidInt);

        if (!EntityManager.TryGetEntity(netEnt, out var target))
        {
            shell.WriteLine(Loc.GetString("shell-could-not-find-entity-with-uid"));
            return;
        }

        if (!_proto.HasIndex<FeedbackPopupPrototype>(args[1]))
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-invalid-proto"));
            return;
        }

        if (!_feedback.SendPopups(target.Value, [args[1]]))
        {
            shell.WriteError(Loc.GetString("feedbackpopup-command-error-popup-send-fail"));
            return;
        }

        shell.WriteLine(Loc.GetString("feedbackpopup-command-success"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<ActorComponent>(args[0]),
                Loc.GetString("feedbackpopup-command-hint-playerUid")),
            2 => CompletionResult.FromHintOptions(_feedback.FeedbackPopupProtoIds,
                Loc.GetString("feedbackpopup-command-hint-protoId")),
            _ => CompletionResult.Empty
        };
    }
}
