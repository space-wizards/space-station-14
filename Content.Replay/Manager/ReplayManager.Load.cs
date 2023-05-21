using Content.Replay.Observer;
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;
using Content.Replay.UI.Loading;
using Content.Replay.UI.Menu;
using Robust.Shared.Utility;

namespace Content.Replay.Manager;

public sealed partial class ReplayManager
{
    public void LoadReplay(IWritableDirProvider dir, ResPath resPath)
    {
        if (CurrentReplay != null)
            StopReplay();

        _controller.TickUpdateOverride += TickUpdateOverride;
        var screen = _stateMan.RequestStateChange<LoadingScreen<ReplayData>>();
        screen.Job = new ContentLoadReplayJob(1/60f, dir, resPath, _loadMan, screen);
        screen.OnJobFinished += OnFinishedLoading;
    }

    private void OnFinishedLoading(ReplayData? data, Exception? ex)
    {
        if (data == null)
        {
            _controller.TickUpdateOverride -= TickUpdateOverride;
            _stateMan.RequestStateChange<ReplayMainScreen>();
            if (ex != null)
                _uiMan.Popup(Loc.GetString("replay-loading-failed", ("reason", ex)));
            return;
        }

        CurrentReplay = data;
        _entMan.EntitySysManager.GetEntitySystem<ReplayObserverSystem>().SetObserverPosition(default);
        RegisterCommands();
    }
}
