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

    private void RegisterCommands()
    {
        _consoleHost.RegisterCommand(PlayCommand, (_, _, _) => Playing = true);
        _consoleHost.RegisterCommand(PauseCommand, (_, _, _) => Playing = false);
        _consoleHost.RegisterCommand(ToggleCommand, (_, _, _) => Playing = !Playing);
        _consoleHost.RegisterCommand(SkipCommand, SkipTicks);
        _consoleHost.RegisterCommand(SetCommand, SetIndex);
    }

    private void UnregisterCommands()
    {
        _consoleHost.UnregisterCommand(PlayCommand);
        _consoleHost.UnregisterCommand(PauseCommand);
        _consoleHost.UnregisterCommand(ToggleCommand);
        _consoleHost.UnregisterCommand(SkipCommand);
        _consoleHost.UnregisterCommand(SetCommand);
    }

    private void SkipTicks(IConsoleShell shell, string argStr, string[] args)
    {
        if (CurrentReplay == null)
            return;

        if (!int.TryParse(args[0], out var ticks))
            return;

        if (ticks == 0)
        {
            Playing = false;
        }

        if (ticks > 0)
        {
            Playing = true;
            Steps ??= 0;
            Steps = Steps + ticks;
            return;
        }

        if (CurrentReplay.RewindDisabled)
            Logger.Warning("Cannot rewind time, blame admemes");
        else
            SetIndex(CurrentReplay.CurrentIndex + ticks, false);
    }

    private void SetIndex(IConsoleShell shell, string argStr, string[] args)
    {
        ActivelyScrubbing = false;
        if (int.TryParse(args[0], out var index))
            SetIndex(index, true);
        else
            shell.WriteError("invalid input");
    }

    public void StopReplay()
    {
        CurrentReplay = null;
        _controller.ContentEntityTickUpdate -= TickUpdate;
        UnregisterCommands();
    }
}
