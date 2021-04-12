#nullable enable
using Content.Client.Interfaces;
using Content.Shared.Audio;
using JetBrains.Annotations;
using Content.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Client;
using Robust.Client.State;
using Content.Client.State;
using Robust.Shared.Player;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BackgroundAudioSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientGameTicker _clientGameTicker = default!;

        private SoundCollectionPrototype _ambientCollection = default!;

        private AudioParams _ambientParams = new(-10f, 1, "Master", 0, 0, true, 0f);
        private AudioParams _lobbyParams = new(-5f, 1, "Master", 0, 0, true, 0f);

        private IPlayingAudioStream? _ambientStream;
        private IPlayingAudioStream? _lobbyStream;

        public override void Initialize()
        {
            base.Initialize();

            _ambientCollection = _prototypeManager.Index<SoundCollectionPrototype>("AmbienceBase");

            _configManager.OnValueChanged(CCVars.AmbienceBasicEnabled, AmbienceCVarChanged);
            _configManager.OnValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);

            _stateManager.OnStateChanged += StateManagerOnStateChanged;

            _client.PlayerJoinedServer += OnJoin;
            _client.PlayerLeaveServer += OnLeave;

            _clientGameTicker.LobbyStatusUpdated += LobbySongReceived;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _stateManager.OnStateChanged -= StateManagerOnStateChanged;

            _client.PlayerJoinedServer -= OnJoin;
            _client.PlayerLeaveServer -= OnLeave;

            _clientGameTicker.LobbyStatusUpdated -= LobbySongReceived;

            EndAmbience();
            EndLobbyMusic();
        }

        private void StateManagerOnStateChanged(StateChangedEventArgs args)
        {
            EndAmbience();
            EndLobbyMusic();
            if (args.NewState is LobbyState && _configManager.GetCVar(CCVars.LobbyMusicEnabled))
            {
                StartLobbyMusic();
            }
            else if (args.NewState is GameScreen && _configManager.GetCVar(CCVars.AmbienceBasicEnabled))
            {
                StartAmbience();
            }
        }

        private void OnJoin(object? sender, PlayerEventArgs args)
        {
            if (_stateManager.CurrentState is LobbyState)
            {
                EndAmbience();
                if (_configManager.GetCVar(CCVars.LobbyMusicEnabled))
                {
                    StartLobbyMusic();
                }
            }
            else
            {
                EndLobbyMusic();
                if (_configManager.GetCVar(CCVars.AmbienceBasicEnabled))
                {
                    StartAmbience();
                }
            }
        }

        private void OnLeave(object? sender, PlayerEventArgs args)
        {
            EndAmbience();
            EndLobbyMusic();
        }

        private void AmbienceCVarChanged(bool ambienceEnabled)
        {
            if (!ambienceEnabled)
            {
                EndAmbience();
            }
            else if (_stateManager.CurrentState is GameScreen)
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
            var file = _robustRandom.Pick(_ambientCollection.PickFiles);
            _ambientStream = SoundSystem.Play(Filter.Local(), file, _ambientParams);
        }

        private void EndAmbience()
        {
            _ambientStream?.Stop();
            _ambientStream = null;
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
            if (_stateManager.CurrentState is LobbyState && _configManager.GetCVar(CCVars.LobbyMusicEnabled))
            {
                StartLobbyMusic();
            }
        }
        private void StartLobbyMusic()
        {
            EndLobbyMusic();
            var file = _clientGameTicker.LobbySong;
            if (file == null) // We have not received the lobby song yet.
            {
                return;
            }
            _lobbyStream = SoundSystem.Play(Filter.Local(), file, _lobbyParams);
        }

        private void EndLobbyMusic()
        {
            _lobbyStream?.Stop();
            _lobbyStream = null;
        }
    }
}
