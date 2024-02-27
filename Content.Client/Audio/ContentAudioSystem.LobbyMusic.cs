using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Shared.Audio.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly ClientGameTicker _gameTicker = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly AudioParams _lobbySongParams = new(-5f, 1, "Master", 0, 0, 0, false, 0f);
    private readonly AudioParams _roundEndSoundEffectParams = new(-5f, 1, "Master", 0, 0, 0, false, 0f);

    private EntityUid? _lobbyMusicStream;
    private EntityUid? _lobbyRoundRestartAudioStream;
    private string[]? _lobbyPlaylist;
    private TimeSpan? _nextSoundtrackOn;
    private string? _currentLobbySoundtrack;
    private Action<LobbySongChangedEvent>? _lobbySongChanged;

    public event Action<LobbySongChangedEvent>? LobbySongChanged
    {
        add
        {
            if (value != null)
            {
                value(new LobbySongChangedEvent(_currentLobbySoundtrack));
                _lobbySongChanged += value;
            }
        }
        remove => _lobbySongChanged -= value;
    }

    private void InitializeLobbyMusic()
    {
        Subs.CVar(_configManager, CCVars.LobbyMusicEnabled, LobbyMusicCVarChanged);
        Subs.CVar(_configManager, CCVars.LobbyMusicVolume, LobbyMusicVolumeCVarChanged);

        _stateManager.OnStateChanged += StateManagerOnStateChanged;

        _client.PlayerLeaveServer += OnLeave;

        SubscribeNetworkEvent<LobbySongStoppedEvent>(OnLobbySongStopped);
        SubscribeNetworkEvent<LobbyPlaylistChangedEvent>(OnLobbySongChanged);
    }

    private void OnLobbySongStopped(LobbySongStoppedEvent ev)
    {
        EndLobbyMusic();
    }

    private void StateManagerOnStateChanged(StateChangedEventArgs args)
    {
        switch (args.NewState)
        {
            case LobbyState:
                if (_lobbyPlaylist != null)
                {
                    StartLobbyPlaylist(_lobbyPlaylist);
                }

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
        if (_lobbyMusicStream.HasValue)
        {
            _audio.SetVolume(_lobbyMusicStream, _lobbySongParams.Volume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume)));
        }
    }

    private void LobbyMusicCVarChanged(bool musicEnabled)
    {
        if (musicEnabled
            && _stateManager.CurrentState is LobbyState
            && _lobbyPlaylist != null
           )
        {
            StartLobbyPlaylist(_lobbyPlaylist);
        }
        else
        {
            EndLobbyMusic();
        }
    }

    private void OnLobbySongChanged(LobbyPlaylistChangedEvent playlistChangedEvent)
    {
        var playlist = playlistChangedEvent.Playlist;
        //playlist is already playing, no need to restart it
        if (_lobbyMusicStream != null
            && _lobbyPlaylist != null
            && _lobbyPlaylist.SequenceEqual(playlist)
           )
        {
            return;
        }

        EndLobbyMusic();
        StartLobbyPlaylist(playlistChangedEvent.Playlist);
    }

    private void StartLobbyPlaylist(string[] playlist)
    {
        if (_lobbyMusicStream != null || !_configManager.GetCVar(CCVars.LobbyMusicEnabled))
            return;

        _lobbyPlaylist = playlist;
        if (_lobbyPlaylist.Length == 0)
        {
            return;
        }

        _currentLobbySoundtrack = playlist[0];

        PlaySoundtrack(_currentLobbySoundtrack);
    }

    private void PlaySoundtrack(string soundtrackFilename)
    {
        if (!_resourceCache.TryGetResource(new ResPath(soundtrackFilename), out AudioResource? audio))
        {
            return;
        }

        _nextSoundtrackOn = _timing.CurTime + audio.AudioStream.Length;

        _lobbyMusicStream = _audio.PlayGlobal(
            soundtrackFilename,
            Filter.Local(),
            false,
            _lobbySongParams.WithVolume(_lobbySongParams.Volume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume)))
        )?.Entity;
        var lobbySongChangedEvent = new LobbySongChangedEvent(soundtrackFilename);
        _lobbySongChanged?.Invoke(lobbySongChangedEvent);
    }

    private void EndLobbyMusic()
    {
        _currentLobbySoundtrack = null;
        _nextSoundtrackOn = null;
        _lobbyMusicStream = _audio.Stop(_lobbyMusicStream);
        var lobbySongChangedEvent = new LobbySongChangedEvent();
        _lobbySongChanged?.Invoke(lobbySongChangedEvent);
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

        _lobbyRoundRestartAudioStream = _audio.PlayGlobal(
            file,
            Filter.Local(),
            false,
            _roundEndSoundEffectParams.WithVolume(_roundEndSoundEffectParams.Volume + SharedAudioSystem.GainToVolume(_configManager.GetCVar(CCVars.LobbyMusicVolume)))
        )?.Entity;
    }

    private void ShutdownLobbyMusic()
    {
        _stateManager.OnStateChanged -= StateManagerOnStateChanged;

        _client.PlayerLeaveServer -= OnLeave;

        EndLobbyMusic();
    }

    private void UpdateLobbyMusic()
    {
        if (
            _nextSoundtrackOn.HasValue
            && _timing.CurTime >= _nextSoundtrackOn
            && _lobbyPlaylist?.Length > 0
            && _currentLobbySoundtrack != null
            )
        {
            _currentLobbySoundtrack = GetNextTrackInLobbyPlaylist(_currentLobbySoundtrack, _lobbyPlaylist);
            PlaySoundtrack(_currentLobbySoundtrack);
        }
    }

    private static string GetNextTrackInLobbyPlaylist(string currentSoundtrack, string[] playlist)
    {
        var indexOfCurrent = Array.IndexOf(playlist, currentSoundtrack);
        var nextTrackIndex = indexOfCurrent + 1;
        if (nextTrackIndex > playlist.Length - 1)
        {
            nextTrackIndex = 0;
        }

        return playlist[nextTrackIndex];
    }
}

public sealed record LobbySongChangedEvent(string? SongFilename = null);
