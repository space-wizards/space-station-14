using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddOverallTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "addoveralltime";
    public string Description => Loc.GetString("cmd-addoveralltime-desc");
    public string Help => Loc.GetString("cmd-addoveralltime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-addoveralltime-error-args"));
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[1])));
            return;
        }

        if (!_playerManager.TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
            return;
        }

        _playTimeTracking.AddTimeToOverallPlaytime(userId, TimeSpan.FromMinutes(minutes));
        var overall = _playTimeTracking.GetOverallPlaytime(userId);

        shell.WriteLine(Loc.GetString(
            "cmd-addoveralltime-succeed",
            ("username", args[0]),
            ("time", overall)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-addoveralltime-arg-user"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-addoveralltime-arg-minutes"));

        return CompletionResult.Empty;
    }
}

public sealed class AddRoleTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "addroletime";
    public string Description => Loc.GetString("cmd-addroletime-desc");
    public string Help => Loc.GetString("cmd-addroletime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("cmd-addroletime-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetUserId(userName, out var userId))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", userName)));
            return;
        }

        var role = args[1];

        var m = args[2];
        if (!int.TryParse(m, out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", minutes)));
            return;
        }

        _playTimeTracking.AddTimeToTracker(userId, role, TimeSpan.FromMinutes(minutes));
        var time = _playTimeTracking.GetOverallPlaytime(userId);
        shell.WriteLine(Loc.GetString("cmd-addroletime-succeed",
            ("username", userName),
            ("role", role),
            ("time", time)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-addroletime-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString("cmd-addroletime-arg-role"));
        }

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString("cmd-addroletime-arg-minutes"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class GetOverallTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "getoveralltime";
    public string Description => Loc.GetString("cmd-getoveralltime-desc");
    public string Help => Loc.GetString("cmd-getoveralltime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-getoveralltime-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetUserId(userName, out var userId))
        {
            shell.WriteError(Loc.GetString("parser-session-fail", ("username", userName)));
            return;
        }

        var value = _playTimeTracking.GetOverallPlaytime(userId);
        shell.WriteLine(Loc.GetString(
            "cmd-getoveralltime-success",
            ("username", userName),
            ("time", value)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-getoveralltime-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class GetRoleTimerCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "getroletimers";
    public string Description => Loc.GetString("cmd-getroletimers-desc");
    public string Help => Loc.GetString("cmd-getroletimers-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            shell.WriteLine(Loc.GetString("cmd-getroletimers-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var session))
        {
            shell.WriteError(Loc.GetString("parser-session-fail", ("username", userName)));
            return;
        }

        if (args.Length == 1)
        {
            var timers = _playTimeTracking.GetTrackerTimes(session.UserId);

            if (timers.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-getroletimers-no"));
                return;
            }

            foreach (var (role, time) in timers)
            {
                shell.WriteLine(Loc.GetString("cmd-getroletimers-role", ("role", role), ("time", time)));
            }
        }

        if (args.Length >= 2)
        {
            if (args[1] == "Overall")
            {
                var timer = _playTimeTracking.GetOverallPlaytime(session.UserId);
                shell.WriteLine(Loc.GetString("cmd-getroletimers-overall", ("time", timer)));
                return;
            }

            var time = _playTimeTracking.GetPlayTimeForTracker(session.UserId, args[1]);
            shell.WriteLine(Loc.GetString("cmd-getroletimers-succeed", ("username", session.Name),
                ("time", time)));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-getroletimers-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString("cmd-getroletimers-arg-role"));
        }

        return CompletionResult.Empty;
    }
}

/// <summary>
/// Saves the timers for a particular player immediately
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class SavePlayTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "savetime";
    public string Description => Loc.GetString("cmd-savetime-desc");
    public string Help => Loc.GetString("cmd-savetime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-savetime-error-args"));
            return;
        }

        var name = args[0];
        if (!_playerManager.TryGetSessionByUsername(name, out var pSession))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", name)));
            return;
        }

        _playTimeTracking.SaveSession(pSession);
        shell.WriteLine(Loc.GetString("cmd-savetime-succeed", ("username", name)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-savetime-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class PlayTimeFlushCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public string Command => "playtime_flush";
    public string Description => Loc.GetString("cmd-playtime_flush-desc");
    public string Help => Loc.GetString("cmd-playtime_flush-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (0 or 1))
        {
            shell.WriteError(Loc.GetString("cmd-playtime_flush-error-args"));
            return;
        }

        if (args.Length == 0)
        {
            _playTimeTracking.FlushAllTrackers();
            return;
        }

        var name = args[0];
        if (!_playerManager.TryGetSessionByUsername(name, out var pSession))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", name)));
            return;
        }

        _playTimeTracking.FlushTracker(pSession);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_flush-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
