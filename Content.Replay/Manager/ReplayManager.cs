using System.Linq;
using Content.Client.RoundEnd;
using Content.Client.UserInterface.Systems.Chat;
using Content.Replay.Observer;
using Content.Replay.UI.Loading;
using Content.Replay.UI.Menu;
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
using Robust.Client.GameObjects;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Replay.Manager;

public sealed class ReplayManager
{
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IClientEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IReplayPlaybackManager _playback = default!;

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
            _stateMan.RequestStateChange<ReplayMainScreen>();
            _uiMan.Popup(Loc.GetString("replay-loading-failed", ("reason", exception)));
        }
    }

    private void OnCheckpointReset()
    {
        // Remove future chat messages when rewinding time.

        // TODO REPLAYS add chat messages when jumping forward in time.
        // Need to allow content to add data to checkpoint states.

        // TODO REPLAYS custom chat control
        // Maybe one that allows players to skip directly to players via their names?
        // I don't like having to just manipulate ChatUiController like this.
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
                case RoundEndMessageEvent roundEnd:

                    if (skipEffects)
                        return true;

                    // TODO REPLAYS handle round end windows properly to prevent window duplication.
                    // The round-end logic just needs to properly track the window. Clients should also be able to
                    // re-open the window after having closed it.

                    if (!_uiMan.WindowRoot.Children.Any(x => x is RoundEndSummaryWindow))
                        _entMan.DispatchReceivedNetworkMsg(roundEnd);
                    return true;
                //
                // TODO REPLAYS figure out a cleaner way of doing this. This sucks.
                // Next: we want to avoid spamming animations, sounds, and pop-ups while scrubbing or rewinding time
                // (e.g., to rewind 1 tick, we really rewind ~60 and then fast forward 59). Currently, this is
                // effectively an EntityEvent blacklist. But this is kinda shit and should be done differently somehow.
                // The unifying aspect of these events is that they trigger pop-ups, UI changes, spawn client-side
                // entities or start animations.
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
        _entMan.EntitySysManager.GetEntitySystem<ReplayObserverSystem>().SetObserverPosition(default);
    }

    private void OnReplayPlaybackStopped()
    {
        _stateMan.RequestStateChange<ReplayMainScreen>();
    }
}
