﻿using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddOverallCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_addoverall";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-error-args"));
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[1])));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
            return;
        }

        _playTimeTracking.AddTimeToOverallPlaytime(player, TimeSpan.FromMinutes(minutes));
        var overall = _playTimeTracking.GetOverallPlaytime(player);

        shell.WriteLine(Loc.GetString(
            $"cmd-{Command}-succeed",
            ("username", args[0]),
            ("time", overall)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString($"cmd-{Command}-arg-user"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-minutes"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddRoleCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_addrole";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
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

        _playTimeTracking.AddTimeToTracker(player, role, TimeSpan.FromMinutes(minutes));
        var time = _playTimeTracking.GetPlayTimeForTracker(player, role);
        shell.WriteLine(Loc.GetString(
            $"cmd-{Command}-succeed",
            ("username", userName),
            ("role", role),
            ("time", time)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString($"cmd-{Command}-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString($"cmd-{Command}-arg-role"));
        }

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString($"cmd-{Command}-arg-minutes"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeGetOverallCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_getoverall";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", userName)));
            return;
        }

        var value = _playTimeTracking.GetOverallPlaytime(player);
        shell.WriteLine(Loc.GetString(
            $"cmd-{Command}-success",
            ("username", userName),
            ("time", value)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString($"cmd-{Command}-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeGetRoleCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_getrole";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            shell.WriteLine(Loc.GetString($"cmd-{Command}-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var session))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", userName)));
            return;
        }

        if (args.Length == 1)
        {
            var timers = _playTimeTracking.GetTrackerTimes(session);

            if (timers.Count == 0)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-no"));
                return;
            }

            foreach (var (role, time) in timers)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-role", ("role", role), ("time", time)));
            }
        }

        if (args.Length >= 2)
        {
            if (args[1] == "Overall")
            {
                var timer = _playTimeTracking.GetOverallPlaytime(session);
                shell.WriteLine(Loc.GetString($"cmd-{Command}-overall", ("time", timer)));
                return;
            }

            var time = _playTimeTracking.GetPlayTimeForTracker(session, args[1]);
            shell.WriteLine(Loc.GetString($"cmd-{Command}-succeed", ("username", session.Name),
                ("time", time)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString($"cmd-{Command}-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString($"cmd-{Command}-arg-role"));
        }

        return CompletionResult.Empty;
    }
}

/// <summary>
/// Saves the timers for a particular player immediately
/// </summary>
[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeSaveCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_save";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString($"cmd-{Command}-error-args"));
            return;
        }

        var name = args[0];
        if (!_playerManager.TryGetSessionByUsername(name, out var pSession))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", name)));
            return;
        }

        _playTimeTracking.SaveSession(pSession);
        shell.WriteLine(Loc.GetString($"cmd-{Command}-succeed", ("username", name)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString($"cmd-{Command}-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class PlayTimeFlushCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    public override string Command => "playtime_flush";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (0 or 1))
        {
            shell.WriteError(Loc.GetString($"cmd-{Command}-error-args"));
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString($"cmd-{Command}-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
