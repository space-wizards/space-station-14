using Content.Client.Administration.Managers;
using Content.Client.Launcher;
using Content.Client.MainMenu;
using Content.Client.Replay.UI.Loading;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Instruments;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Replay;

public sealed class ContentReplayPlaybackManager
{
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IGameController _controller = default!;
    [Dependency] private readonly IClientEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IReplayPlaybackManager _playback = default!;
    [Dependency] private readonly IClientConGroupController _conGrp = default!;
    [Dependency] private readonly IClientAdminManager _adminMan = default!;

    /// <summary>
    /// UI state to return to when stopping a replay or loading fails.
    /// </summary>
    public Type? DefaultState;

    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _playback.HandleReplayMessage += OnHandleReplayMessage;
        _playback.ReplayPlaybackStopped += OnReplayPlaybackStopped;
        _playback.ReplayPlaybackStarted += OnReplayPlaybackStarted;
        _playback.ReplayCheckpointReset += OnCheckpointReset;
        _loadMan.LoadOverride += LoadOverride;
    }

    private void LoadOverride(IWritableDirProvider dir, ResPath resPath)
    {
        var screen = _stateMan.RequestStateChange<LoadingScreen<bool>>();
        screen.Job = new ContentLoadReplayJob(1/60f, dir, resPath, _loadMan, screen);
        screen.OnJobFinished += (_, e) => OnFinishedLoading(e);
    }

    private void OnFinishedLoading(Exception? exception)
    {
        if (exception != null)
        {
            ReturnToDefaultState();
            _uiMan.Popup(Loc.GetString("replay-loading-failed", ("reason", exception)));
        }
    }

    public void ReturnToDefaultState()
    {
        if (DefaultState != null)
            _stateMan.RequestStateChange(DefaultState);
        else if (_controller.LaunchState.FromLauncher)
            _stateMan.RequestStateChange<LauncherConnecting>().SetDisconnected();
        else
            _stateMan.RequestStateChange<MainScreen>();
    }

    private void OnCheckpointReset()
    {
        // This function removes future chat messages when rewinding time.

        // TODO REPLAYS add chat messages when jumping forward in time.
        // Need to allow content to add data to checkpoint states.

        _uiMan.GetUIController<ChatUIController>().History.RemoveAll(x => x.Item1 > _timing.CurTick);
        _uiMan.GetUIController<ChatUIController>().Repopulate();
    }

    private bool OnHandleReplayMessage(object message, bool skipEffects)
    {
        switch (message)
            {
                case ChatMessage chat:
                    // Just pass on the chat message to the UI controller, but skip speech-bubbles if we are fast-forwarding.
                    _uiMan.GetUIController<ChatUIController>().ProcessChatMessage(chat, speechBubble: !skipEffects);
                    return true;
                // TODO REPLAYS figure out a cleaner way of doing this. This sucks.
                // Next: we want to avoid spamming animations, sounds, and pop-ups while scrubbing or rewinding time
                // (e.g., to rewind 1 tick, we really rewind ~60 and then fast forward 59). Currently, this is
                // effectively an EntityEvent blacklist. But this is kinda shit and should be done differently somehow.
                // The unifying aspect of these events is that they trigger pop-ups, UI changes, spawn client-side
                // entities or start animations.
                case RoundEndMessageEvent:
                case PopupEvent:
                case AudioMessage:
                case PickupAnimationEvent:
                case MeleeLungeEvent:
                case SharedGunSystem.HitscanEvent:
                case ImpactEffectEvent:
                case MuzzleFlashEvent:
                case DamageEffectEvent:
                case InstrumentStartMidiEvent:
                case InstrumentMidiEventEvent:
                case InstrumentStopMidiEvent:
                    if (!skipEffects)
                        _entMan.DispatchReceivedNetworkMsg((EntityEventArgs)message);
                    return true;
            }

        return false;
    }


    private void OnReplayPlaybackStarted()
    {
        _conGrp.Implementation = new ReplayConGroup();
    }

    private void OnReplayPlaybackStopped()
    {
        _conGrp.Implementation = (IClientConGroupImplementation)_adminMan;
        ReturnToDefaultState();
    }
}
