#nullable enable
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
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

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AmbienceSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IBaseClient _client = default!;

        private AudioSystem _audioSystem = default!;

        private SoundCollectionPrototype _ambientCollection = default!;
        private SoundCollectionPrototype _lobbyCollection = default!;

        private AudioParams _ambientParams = new(-10f, 1, "Master", 0, 0, AudioMixTarget.Stereo, true, 0f);
        private AudioParams _lobbyParams = new(5f, 1, "Master", 0, 0, AudioMixTarget.Stereo, true, 0f);

        private IPlayingAudioStream? _ambientStream;
        private IPlayingAudioStream? _lobbyStream;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystemManager.GetEntitySystem<AudioSystem>();

            _ambientCollection = _prototypeManager.Index<SoundCollectionPrototype>("AmbienceBase");
            _lobbyCollection = _prototypeManager.Index<SoundCollectionPrototype>("LobbyMusic");

            _configManager.OnValueChanged(CCVars.AmbienceBasicEnabled, AmbienceCVarChanged);
            _configManager.OnValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);

            _stateManager.OnStateChanged += StateManagerOnStateChanged;

            _client.PlayerJoinedServer += OnJoin;
            _client.PlayerLeaveServer += OnLeave;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _stateManager.OnStateChanged -= StateManagerOnStateChanged;

            _client.PlayerJoinedServer -= OnJoin;
            _client.PlayerLeaveServer -= OnLeave;

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
        }

        private void StartAmbience()
        {
            EndAmbience();
            var file = _robustRandom.Pick(_ambientCollection.PickFiles);
            _ambientStream = _audioSystem.Play(file, _ambientParams);
        }

        private void EndAmbience()
        {
            if (_ambientStream == null)
            {
                return;
            }
            _ambientStream.Stop();
            _ambientStream = null;
        }

        private void LobbyMusicCVarChanged(bool musicEnabled)
        {
            if (!musicEnabled)
            {
                EndLobbyMusic();
            }
            else if (_stateManager.CurrentState is GameScreen)
            {
                StartLobbyMusic();
            }
        }

        private void StartLobbyMusic()
        {
            EndLobbyMusic();
            var file = _robustRandom.Pick(_lobbyCollection.PickFiles);
            _lobbyStream = _audioSystem.Play(file, _lobbyParams);
        }

        private void EndLobbyMusic()
        {
            if (_lobbyStream == null)
            {
                return;
            }
            _lobbyStream.Stop();
            _lobbyStream = null;
        }
    }
}

