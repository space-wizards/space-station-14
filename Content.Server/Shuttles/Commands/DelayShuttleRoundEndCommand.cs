using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.RoundEnd;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Delays the round from ending via the shuttle call. Can still be ended via other means.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class DelayRoundEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!; // DS14
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!; // DS14

    public override string Command => "delayroundend";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // DS14-start
        switch (args.Length)
        {
            case 0:
                if (_roundEndSystem.ToggleRoundTransitionTimer(out var paused))
                {
                    var message = Loc.GetString(
                        paused ? "cmd-delayroundend-paused" : "cmd-delayroundend-resumed",
                        ("restart", _roundEndSystem.RoundTransitionRestartsRound));
                    shell.WriteLine(message);
                    _chatManager.DispatchServerAnnouncement(message);
                }
                else
                    shell.WriteLine(Loc.GetString("cmd-delayroundend-no-timer"));
                return;
            case 1:
                break;
            default:
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
        }

        if (!int.TryParse(args[0], out var seconds) || seconds == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-delayroundend-invalid-seconds", ("value", args[0])));
            return;
        }

        if (_roundEndSystem.AdjustRoundTransitionTimer(TimeSpan.FromSeconds(seconds)))
        {
            var message = Loc.GetString(
                seconds > 0 ? "cmd-delayroundend-extended" : "cmd-delayroundend-shortened",
                ("seconds", Math.Abs((long) seconds)),
                ("restart", _roundEndSystem.RoundTransitionRestartsRound));
            shell.WriteLine(message);
            _chatManager.DispatchServerAnnouncement(message);
        }
        else
            shell.WriteLine(Loc.GetString("cmd-delayroundend-no-timer"));
        // DS14-end
    }
}
