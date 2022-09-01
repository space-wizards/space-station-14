using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Threading;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Audio
{
    [UsedImplicitly]
    public sealed class BackgroundAudioSystem : EntitySystem
    {
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly ClientGameTicker _gameTicker = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        private const float AmbientVolume = -10f;
        private const float AmbientVolumeQuiet = -25f;

        private readonly AudioParams _ambientParams = new(AmbientVolume, 1, "Master", 0, 0, 0, true, 0f);
        private readonly AudioParams _lobbyParams = new(-5f, 1, "Master", 0, 0, 0, true, 0f);

        private AudioSystem.PlayingStream? _ambientStream;
        private AudioSystem.PlayingStream? _lobbyStream;

        /// <summary>
        /// What is currently playing.
        /// </summary>
        private SoundCollectionPrototype? _playingCollection;

        /// <summary>
        /// What the ambience has been set to.
        /// </summary>
        private SoundCollectionPrototype? _currentCollection;
        private CancellationTokenSource _timerCancelTokenSource = new();

        private SoundCollectionPrototype _spaceAmbience = default!;
        private SoundCollectionPrototype _stationAmbience = default!;

        // TODO continue background music when transferring between space and station

        private float _transitionVolume = AmbientVolume;
        private float _transitionFadeTimeSeconds = 5f;
        private float _transitionFadeTime;

        private int _transitionTime = 2000;
        private TransitionState _transitioning = TransitionState.None;

        private enum TransitionState
        {
            None,
            In,
            Out,
        }

        public override void Initialize()
        {
            base.Initialize();

            _stationAmbience = _prototypeManager.Index<SoundCollectionPrototype>("StationAmbienceBase");
            _spaceAmbience = _prototypeManager.Index<SoundCollectionPrototype>("SpaceAmbienceBase");
            _currentCollection = _stationAmbience;
            _transitionFadeTime = (AmbientVolume - AmbientVolumeQuiet ) / _transitionFadeTimeSeconds;

            // TODO: Ideally audio loading streamed better / we have more robust audio but this is quite annoying
            var cache = IoCManager.Resolve<IResourceCache>();

            foreach (var audio in _spaceAmbience.PickFiles)
            {
                cache.GetResource<AudioResource>(audio.ToString());
            }

            _configManager.OnValueChanged(CCVars.AmbienceVolume, AmbienceCVarChanged);
            _configManager.OnValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
            _configManager.OnValueChanged(CCVars.StationAmbienceEnabled, StationAmbienceCVarChanged);
            _configManager.OnValueChanged(CCVars.SpaceAmbienceEnabled, SpaceAmbienceCVarChanged);

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

            _stateManager.OnStateChanged += StateManagerOnStateChanged;

            _client.PlayerJoinedServer += OnJoin;
            _client.PlayerLeaveServer += OnLeave;

            _gameTicker.LobbyStatusUpdated += LobbySongReceived;
        }

        private void OnPlayerAttached(PlayerAttachedEvent ev)
        {
            if (!TryComp<TransformComponent>(ev.Entity, out var xform))
                return;

            CheckAmbience(xform);
        }

        private void OnPlayerDetached(PlayerDetachedEvent ev)
        {
            EndAmbience();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _configManager.UnsubValueChanged(CCVars.AmbienceVolume, AmbienceCVarChanged);
            _configManager.UnsubValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
            _configManager.UnsubValueChanged(CCVars.StationAmbienceEnabled, StationAmbienceCVarChanged);
            _configManager.UnsubValueChanged(CCVars.SpaceAmbienceEnabled, SpaceAmbienceCVarChanged);

            _stateManager.OnStateChanged -= StateManagerOnStateChanged;

            _client.PlayerJoinedServer -= OnJoin;
            _client.PlayerLeaveServer -= OnLeave;

            _gameTicker.LobbyStatusUpdated -= LobbySongReceived;

            EndAmbience();
            EndLobbyMusic();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_timing.InSimulation)
                return;

            switch (_transitioning)
            {
                case TransitionState.None:
                    return;
                case TransitionState.In:
                    _transitionVolume += frameTime * _transitionFadeTime;
                    break;
                case TransitionState.Out:
                    _transitionVolume -= frameTime * _transitionFadeTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_transitioning == TransitionState.Out && _transitionVolume < AmbientVolumeQuiet)
            {
                _transitioning = TransitionState.None;
                _transitionVolume = AmbientVolumeQuiet;
            }
            if (_transitioning == TransitionState.In && _transitionVolume > AmbientVolume)
            {
                _transitioning = TransitionState.None;
                _transitionVolume = AmbientVolume;
            }

            if (_ambientStream != null)
                _ambientStream.Source.SetVolume(_transitionVolume);
        }

        private void CheckAmbience(TransformComponent xform)
        {
            if (xform.GridUid != null)
            {
                if (_currentCollection == _stationAmbience)
                    return;
                ChangeAmbience(_stationAmbience);
            }
            else
            {
                ChangeAmbience(_spaceAmbience);
            }
        }

        private void EntParentChanged(ref EntParentChangedMessage message)
        {
            if(_playMan.LocalPlayer is null
               || _playMan.LocalPlayer.ControlledEntity != message.Entity
               || !_timing.IsFirstTimePredicted) return;

            // Check if we traversed to grid.
            CheckAmbience(message.Transform);
        }

        private void ChangeAmbience(SoundCollectionPrototype newAmbience)
        {
            if (_currentCollection == newAmbience) return;
            _timerCancelTokenSource.Cancel();
            _currentCollection = newAmbience;
            _timerCancelTokenSource = new();
            _transitioning = TransitionState.Out;
            Timer.Spawn(_playingCollection != null ? _transitionTime : 1, () =>
            {
                _transitioning = TransitionState.In;
                // If we traverse a few times then don't interrupt an existing song.
                if (_playingCollection == _currentCollection) return;
                StartAmbience();
            }, _timerCancelTokenSource.Token);
        }

        private void StateManagerOnStateChanged(StateChangedEventArgs args)
        {
            EndAmbience();

            if (args.NewState is LobbyState)
            {
                StartLobbyMusic();
                return;
            }
            else if (args.NewState is GameScreen)
            {
                StartAmbience();
            }

            EndLobbyMusic();
        }

        private void OnJoin(object? sender, PlayerEventArgs args)
        {
            if (_stateManager.CurrentState is LobbyState)
            {
                EndAmbience();
                StartLobbyMusic();
            }
            else
            {
                EndLobbyMusic();
                StartAmbience();
            }
        }

        private void OnLeave(object? sender, PlayerEventArgs args)
        {
            EndAmbience();
            EndLobbyMusic();
        }

        private void AmbienceCVarChanged(float volume)
        {
            if (_stateManager.CurrentState is GameScreen)
            {
                StartAmbience();
            }
            else
            {
                EndAmbience();
            }
        }

        private void StartAmbience()
        {
            EndAmbience();
            if (_currentCollection == null || !CanPlayCollection(_currentCollection))
                return;
            _playingCollection = _currentCollection;
            var file = _robustRandom.Pick(_currentCollection.PickFiles).ToString();
            if (_audioSystem.PlayGlobal(file, Filter.Local(),
                _ambientParams.WithVolume(_ambientParams.Volume + _configManager.GetCVar(CCVars.AmbienceVolume)))
                as AudioSystem.PlayingStream is not { } playingStream)
                return;
            _ambientStream = playingStream;
        }

        private void EndAmbience()
        {
            _playingCollection = null;
            _ambientStream?.Stop();
            _ambientStream = null;
        }

        private bool CanPlayCollection(SoundCollectionPrototype collection)
        {
            if (collection.ID == _spaceAmbience.ID)
                return _configManager.GetCVar(CCVars.SpaceAmbienceEnabled);
            if (collection.ID == _stationAmbience.ID)
                return _configManager.GetCVar(CCVars.StationAmbienceEnabled);

            return true;
        }

        private void StationAmbienceCVarChanged(bool enabled)
        {
            if (_currentCollection == null)
                return;

            if (enabled && _stateManager.CurrentState is GameScreen && _currentCollection.ID == _stationAmbience.ID)
            {
                StartAmbience();
            }
            else if(_currentCollection.ID == _stationAmbience.ID)
            {
                EndAmbience();
            }
        }

        private void SpaceAmbienceCVarChanged(bool enabled)
        {
            if (_currentCollection == null)
                return;

            if (enabled && _stateManager.CurrentState is GameScreen && _currentCollection.ID == _spaceAmbience.ID)
            {
                StartAmbience();
            }
            else if(_currentCollection.ID == _spaceAmbience.ID)
            {
                EndAmbience();
            }
        }

        private void LobbyMusicCVarChanged(bool musicEnabled)
        {
            if (!musicEnabled)
            {
                EndLobbyMusic();
            }
            else if (_stateManager.CurrentState is LobbyState)
            {
                StartLobbyMusic();
            }
            else
            {
                EndLobbyMusic();
            }
        }

        private void LobbySongReceived()
        {
            if (_lobbyStream != null) //Toggling Ready status fires this method. This check ensures we only start the lobby music if it's not playing.
            {
                return;
            }
            if (_stateManager.CurrentState is LobbyState)
            {
                StartLobbyMusic();
            }
        }

        public void RestartLobbyMusic()
        {
            EndLobbyMusic();
            StartLobbyMusic();
        }

        public void StartLobbyMusic()
        {
            if (_lobbyStream != null || !_configManager.GetCVar(CCVars.LobbyMusicEnabled)) return;

            var file = _gameTicker.LobbySong;
            if (file == null) // We have not received the lobby song yet.
            {
                return;
            }
            if (_audioSystem.PlayGlobal(file, Filter.Local(), _lobbyParams) as AudioSystem.PlayingStream is not {} playingStream)
                return;
            _lobbyStream = playingStream;
        }

        private void EndLobbyMusic()
        {
            _lobbyStream?.Stop();
            _lobbyStream = null;
        }
    }
}
