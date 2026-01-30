using Content.Shared.Administration;
using Content.Shared.Tips;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class TippyCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedTipsSystem _tips = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override string Command => "tippy";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("cmd-tippy-help"));
            return;
        }

        ICommonSession? targetSession = null;
        if (args[0] != "all")
        {
            if (!_player.TryGetSessionByUsername(args[0], out targetSession))
            {
                shell.WriteLine(Loc.GetString("cmd-tippy-error-no-user"));
                return;
            }
        }

        var msg = args[1];

        EntProtoId? prototype = null;
        if (args.Length > 2)
        {
            if (args[2] == "null")
                prototype = null;
            else if (!_prototype.HasIndex<EntityPrototype>(args[2]))
            {
                shell.WriteError(Loc.GetString("cmd-tippy-error-no-prototype", ("proto", args[2])));
                return;
            }
            else
                prototype = args[2];
        }

        var speakTime = _tips.GetSpeechTime(msg);
        var slideTime = 3f;
        var waddleInterval = 0.5f;

        if (args.Length > 3 && float.TryParse(args[3], out var parsedSpeakTime))
            speakTime = parsedSpeakTime;

        if (args.Length > 4 && float.TryParse(args[4], out var parsedSlideTime))
            slideTime = parsedSlideTime;

        if (args.Length > 5 && float.TryParse(args[5], out var parsedWaddleInterval))
            waddleInterval = parsedWaddleInterval;

        if (targetSession != null) // send to specified player
            _tips.SendTippy(targetSession, msg, prototype, speakTime, slideTime, waddleInterval);
        else // send to everyone
            _tips.SendTippy(msg, prototype, speakTime, slideTime, waddleInterval);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _player),
                Loc.GetString("cmd-tippy-auto-1")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-2")),
            3 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIdsLimited<EntityPrototype>(args[2], _prototype),
                Loc.GetString("cmd-tippy-auto-3")),
            4 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-4")),
            5 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-5")),
            6 => CompletionResult.FromHint(Loc.GetString("cmd-tippy-auto-6")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class TipCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedTipsSystem _tips = default!;

    public override string Command => "tip";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _tips.AnnounceRandomTip();
        _tips.RecalculateNextTipTime();
    }
}
