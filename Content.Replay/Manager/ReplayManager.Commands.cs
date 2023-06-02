using Robust.Shared.Console;

namespace Content.Replay.Manager;

// This partial class has code for all the replay console commands.
public sealed partial class ReplayManager
{
    public const string PlayCommand = "replay_play";
    public const string PauseCommand = "replay_pause";
    public const string ToggleCommand = "replay_toggle";
    public const string SkipCommand = "replay_skip";
    public const string SetCommand = "replay_set";
    public const string StopCommand = "replay_stop";

    private void RegisterCommands()
    {
        _consoleHost.RegisterCommand(PlayCommand,
            Loc.GetString("cmd-replay-play-desc"),
            Loc.GetString("cmd-replay-play-help"),
            (_, _, _) => Playing = true);

        _consoleHost.RegisterCommand(PauseCommand,
            Loc.GetString("cmd-replay-pause-desc"),
            Loc.GetString("cmd-replay-pause-help"),
            (_, _, _) => Playing = false);

        _consoleHost.RegisterCommand(ToggleCommand,
            Loc.GetString("cmd-replay-toggle-desc"),
            Loc.GetString("cmd-replay-toggle-help"),
            (_, _, _) => Playing = !Playing);

        _consoleHost.RegisterCommand(SkipCommand,
            Loc.GetString("cmd-replay-skip-desc"),
            Loc.GetString("cmd-replay-skip-help"),
            OnSkipCommand,
            SkipCommandCompletion);

        _consoleHost.RegisterCommand(SetCommand,
            Loc.GetString("cmd-replay-set-desc"),
            Loc.GetString("cmd-replay-set-help"),
            OnSetCommand,
            SetCommandCompletion);

        _consoleHost.RegisterCommand(StopCommand,
            Loc.GetString("cmd-replay-stop-desc"),
            Loc.GetString("cmd-replay-stop-help"),
            (_, _, _) => StopReplay());
    }

    private CompletionResult SkipCommandCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHint(Loc.GetString("cmd-replay-skip-hint"));
    }

    private CompletionResult SetCommandCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHint(Loc.GetString("cmd-replay-set-hint"));
    }

    private void UnregisterCommands()
    {
        _consoleHost.UnregisterCommand(PlayCommand);
        _consoleHost.UnregisterCommand(PauseCommand);
        _consoleHost.UnregisterCommand(ToggleCommand);
        _consoleHost.UnregisterCommand(SkipCommand);
        _consoleHost.UnregisterCommand(SetCommand);
        _consoleHost.UnregisterCommand(StopCommand);
    }

    private void OnSkipCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (CurrentReplay == null)
            return;

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (int.TryParse(args[0], out var ticks))
        {
            if (ticks < 0)
                SetIndex(CurrentReplay.CurrentIndex + ticks, false);
            else if (ticks == 0)
                Playing = false;
            else
            {
                Playing = true;
                PlaybackLimit ??= 0;
                PlaybackLimit += ticks;
            }

            return;
        }

        if (!TimeSpan.TryParse(args[0], out var time))
        {
            shell.WriteError(Loc.GetString("cmd-replay-error-time", ("time", args[0])));
            return;
        }

        var target = CurrentReplay.CurTime + time;
        var index = Array.BinarySearch(CurrentReplay.ServerTime, target);

        if (index < 0)
            index = Math.Max(0, ~index - 1);

        SetIndex(index, true);
    }

    private void OnSetCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (CurrentReplay == null)
            return;

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        ActivelyScrubbing = false;
        if (int.TryParse(args[0], out var index))
        {
            SetIndex(index, true);
            return;
        }

        if (!TimeSpan.TryParse(args[0], out var target))
        {
            shell.WriteError(Loc.GetString("cmd-replay-error-time", ("time", args[0])));
            return;
        }

        index = Array.BinarySearch(CurrentReplay.ServerTime, target);

        if (index < 0)
            index = Math.Max(0, ~index - 1);

        SetIndex(index, true);
    }
}
