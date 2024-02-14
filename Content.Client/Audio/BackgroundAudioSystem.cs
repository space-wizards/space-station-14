using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Audio;

[UsedImplicitly]
public sealed class BackgroundAudioSystem : EntitySystem
{
    /*
     * TODO: Nuke this system and merge into contentaudiosystem
     */
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly ClientGameTicker _gameTicker = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private readonly AudioParams _lobbyParams = new(-5f, 1, "Master", 0, 0, 0, true, 0f);
    private readonly AudioParams _roundEndParams = new(-5f, 1, "Master", 0, 0, 0, false, 0f);

    public EntityUid? LobbyMusicStream;
    public EntityUid? LobbyRoundRestartAudioStream;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
        Subs.CVar(_configManager, CCVars.LobbyMusicVolume, LobbyMusicVolumeCVarChanged);

        _stateManager.OnStateChanged += StateManagerOnStateChanged;

        _client.PlayerLeaveServer += OnLeave;

        _gameTicker.LobbySongUpdated += LobbySongUpdated;

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(PlayRestartSound);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _stateManager.OnStateChanged -= StateManagerOnStateChanged;

        _client.PlayerLeaveServer -= OnLeave;

        _gameTicker.LobbySongUpdated -= LobbySongUpdated;

        EndLobbyMusic();
    }

    private void StateManagerOnStateChanged(StateChangedEventArgs args)
    {
        switch (args.NewState)
        {
            case LobbyState:
                StartLobbyMusic();
                break;
            default:
                EndLobbyMusic();
                break;
        }
    }

    private void OnLeave(object? sender, PlayerEventArgs args)
    {
        EndLobbyMusic();
    }

    private void LobbyMusicVolumeCVarChanged(float volume)
    {
        if (_stateManager.CurrentState is LobbyState)
        {
            RestartLobbyMusic();
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

    private void LobbySongUpdated()
    {
        RestartLobbyMusic();
    }

    public void RestartLobbyMusic()
    {
        EndLobbyMusic();
        StartLobbyMusic();
    }

    public void StartLobbyMusic()
    {
        if (LobbyMusicStream != null || !_configManager.GetCVar(CCVars.LobbyMusicEnabled))
            return;

        var file = _gameTicker.LobbySong;
        if (file == null) // We have not received the lobby song yet.
        {
            return;
        }

        LobbyMusicStream = _audio.PlayGlobal(
            file,
            Filter.Local(),
            false,
            _lobbyParams.WithVolume(_lobbyParams.Volume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume))))?.Entity;
    }

    private void EndLobbyMusic()
    {
        LobbyMusicStream = _audio.Stop(LobbyMusicStream);
    }

    private void PlayRestartSound(RoundRestartCleanupEvent ev)
    {
        if (!_configManager.GetCVar(CCVars.RestartSoundsEnabled))
            return;

        var file = _gameTicker.RestartSound;
        if (string.IsNullOrEmpty(file))
        {
            return;
        }

        LobbyRoundRestartAudioStream = _audio.PlayGlobal(
            file,
            Filter.Local(),
            false,
            _roundEndParams.WithVolume(_roundEndParams.Volume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume)))
        )?.Entity;
    }
}
