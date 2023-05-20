using Content.Replay.UI.Menu;
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
        _consoleHost.RegisterCommand(StopCommand, (_, _, _) => StopReplay());
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
        _consoleHost.UnregisterCommand(StopCommand);
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
        _controller.TickUpdateOverride -= TickUpdateOverride;
        UnregisterCommands();
        _entMan.FlushEntities();
        _stateMan.RequestStateChange<ReplayMainScreen>();

        // TODO REPLAYS unload extra prototypes
        // TODO REPLAYS unload extra resources.
    }
}
