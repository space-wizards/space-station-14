using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client.Audio;

/// <summary>
/// Handles cross-round audio, like lobby music and the restart sound effect.
/// </summary>
[UsedImplicitly]
public sealed partial class ContentAudioSystem
{
    // Default audio parameters for background audio.
    private const float BaseVolume = -5;
    private const float BaseRestartVolume = 0;
    private const float BasePitch = 1;
    private const string BusName = "Master";
    private const float MaxDistance = 0;
    private const float RolloffFactor = 0;
    private const float RefDistance = 0;
    private const float PlayOffsetSeconds = 0;

    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly ClientGameTicker _gameTicker = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private EntityUid? _lobbyMusicStream;
    private EntityUid? _lobbyRoundRestartAudioStream;

    private void InitializeBackgroundAudio()
    {
        _configManager.OnValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
        _configManager.OnValueChanged(CCVars.LobbyMusicVolume, LobbyMusicVolumeCVarChanged);

        _stateManager.OnStateChanged += StateManagerOnStateChanged;

        _client.PlayerLeaveServer += OnLeave;

        _gameTicker.LobbySongUpdated += LobbySongUpdated;

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        PlayRestartSound();

        _fadingOut.Clear();

        // Preserve lobby sfx but everything else should get dumped.
        TryComp(_lobbyMusicStream, out AudioComponent? lobbyMusicComp);
        var oldMusicGain = lobbyMusicComp?.Gain;

        TryComp(_lobbyRoundRestartAudioStream, out AudioComponent? restartComp);
        var oldAudioGain = restartComp?.Gain;

        SilenceAudio();

        if (oldMusicGain != null)
        {
            Audio.SetGain(_lobbyMusicStream, oldMusicGain.Value, lobbyMusicComp);
        }

        if (oldAudioGain != null)
        {
            Audio.SetGain(_lobbyRoundRestartAudioStream, oldAudioGain.Value, restartComp);
        }
    }

    private void ShutdownBackgroundAudio()
    {
        _configManager.UnsubValueChanged(CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
        _configManager.UnsubValueChanged(CCVars.LobbyMusicVolume, LobbyMusicVolumeCVarChanged);

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
            RestartLobbyMusic();
    }

    private void LobbyMusicCVarChanged(bool musicEnabled)
    {
        if (!musicEnabled)
            EndLobbyMusic();
        else if (_stateManager.CurrentState is LobbyState)
            StartLobbyMusic();
        else
            EndLobbyMusic();
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
        if (_lobbyMusicStream != null || !_configManager.GetCVar(CCVars.LobbyMusicEnabled))
            return;

        var file = _gameTicker.LobbySong;
        if (file == null)
            return;

        var volume = BaseVolume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume));
        AudioParams lobbyParams = new(volume, BasePitch, BusName, MaxDistance, RolloffFactor, RefDistance, true, PlayOffsetSeconds);

        _lobbyMusicStream = _audio.PlayGlobal(file, Filter.Local(), false, lobbyParams)?.Entity;
    }

    private void EndLobbyMusic()
    {
        _lobbyMusicStream = _audio.Stop(_lobbyMusicStream);
    }

    private void PlayRestartSound()
    {
        if (!_configManager.GetCVar(CCVars.RestartSoundsEnabled))
            return;

        var file = _gameTicker.RestartSound;
        if (string.IsNullOrEmpty(file))
            return;

        var volume = BaseRestartVolume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume));
        AudioParams roundEndParams = new(volume, BasePitch, BusName, MaxDistance, RolloffFactor, RefDistance, false, PlayOffsetSeconds);

        _lobbyRoundRestartAudioStream = _audio.PlayGlobal(file, Filter.Local(), false, roundEndParams)?.Entity;
    }
}
