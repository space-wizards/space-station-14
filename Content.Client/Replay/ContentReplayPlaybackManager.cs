using System.IO.Compression;
using Content.Client.Administration.Managers;
using Content.Client.Launcher;
using Content.Client.MainMenu;
using Content.Client.Replay.Spectator;
using Content.Client.Replay.UI.Loading;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Effects;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Hands;
using Content.Shared.Instruments;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Markdown.Mapping;
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
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

    /// <summary>
    /// UI state to return to when stopping a replay or loading fails.
    /// </summary>
    public Type? DefaultState;

    public bool IsScreenshotMode = false;

    private bool _initialized;
    
    /// <summary>
    /// Most recently loaded file, for re-attempting the load with error tolerance.
    /// Required because the zip reader auto-disposes and I'm too lazy to change it so that
    /// <see cref="ReplayFileReaderZip"/> can re-open it.
    /// </summary>
    public (ResPath? Zip, ResPath Folder)? LastLoad;

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

    private void LoadOverride(IReplayFileReader fileReader)
    {
        var screen = _stateMan.RequestStateChange<LoadingScreen<bool>>();
        screen.Job = new ContentLoadReplayJob(1 / 60f, fileReader, _loadMan, screen);
        screen.OnJobFinished += (_, e) => OnFinishedLoading(e);
    }

    private void OnFinishedLoading(Exception? exception)
    {
        if (exception == null)
        {
            LastLoad = null;
            return;
        }

        ReturnToDefaultState();

        // Show a popup window with the error message
        var text = Loc.GetString("replay-loading-failed", ("reason", exception));
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = {new Label {Text = text}}
        };

        var popup = new DefaultWindow { Title = "Error!" };
        popup.Contents.AddChild(box);

        // Add button for attempting to re-load the replay while ignoring some errors.
        if (!_cfg.GetCVar(CVars.ReplayIgnoreErrors) && LastLoad is {} last)
        {
            var button = new Button
            {
                Text = Loc.GetString("replay-loading-retry"), 
                StyleClasses = { StyleBase.ButtonCaution }
            };
            
            button.OnPressed += _ =>
            {
                _cfg.SetCVar(CVars.ReplayIgnoreErrors, true);
                popup.Dispose();

                IReplayFileReader reader = last.Zip == null
                    ? new ReplayFileReaderResources(_resMan, last.Folder)
                    : new ReplayFileReaderZip(new(_resMan.UserData.OpenRead(last.Zip.Value)), last.Folder);

                _loadMan.LoadAndStartReplay(reader);
            };
            
            box.AddChild(button);
        }

        popup.OpenCentered();
    }

    public void ReturnToDefaultState()
    {
        if (DefaultState != null)
            _stateMan.RequestStateChange(DefaultState);
        else if (_controller.LaunchState.FromLauncher)
            _stateMan.RequestStateChange<LauncherConnecting>().SetDisconnected();
        else
            _stateMan.RequestStateChange<MainScreen>();

        if (_client.RunLevel == ClientRunLevel.SinglePlayerGame)
            _client.StopSinglePlayer();
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
        // TODO REPLAYS figure out a cleaner way of doing this. This sucks.
        // Maybe wrap the event in another cancellable event and raise that?

        // This is where replays filter through networked messages and can choose to ignore or give them special treatment.
        // In particular, we want to avoid spamming pop-ups, sounds, and visual effect entities while fast forwarding.
        // E.g., when rewinding 1 tick, we really rewind back to the last checkpoint and then fast forward. Currently, this is
        // effectively an EntityEvent blacklist.

        switch (message)
        {
            case BoundUserInterfaceMessage: // TODO REPLAYS refactor BUIs
            case RequestWindowAttentionEvent:
                // Mark as handled -- the event won't get raised.
                return true;
            case TickerJoinGameEvent:
                if (!_entMan.EntityExists(_player.LocalEntity))
                    _entMan.System<ReplaySpectatorSystem>().SetSpectatorPosition(default);
                return true;
            case ChatMessage chat:
                _uiMan.GetUIController<ChatUIController>().ProcessChatMessage(chat, speechBubble: !skipEffects);
                return true;
        }

        if (!skipEffects)
        {
            // Don't mark as handled -- the event get raised as a normal networked event.
            return false;
        }

        switch (message)
        {
            case RoundEndMessageEvent:
            case PopupEvent:
            case PickupAnimationEvent:
            case MeleeLungeEvent:
            case SharedGunSystem.HitscanEvent:
            case ImpactEffectEvent:
            case MuzzleFlashEvent:
            case ColorFlashEffectEvent:
            case InstrumentStartMidiEvent:
            case InstrumentMidiEventEvent:
            case InstrumentStopMidiEvent:
                // Block visual effects, pop-ups, and sounds
                return true;
        }

        return false;
    }

    private void OnReplayPlaybackStarted(MappingDataNode metadata, List<object> objects)
    {
        _conGrp.Implementation = new ReplayConGroup();
    }

    private void OnReplayPlaybackStopped()
    {
        _conGrp.Implementation = (IClientConGroupImplementation) _adminMan;
        ReturnToDefaultState();
    }
}
