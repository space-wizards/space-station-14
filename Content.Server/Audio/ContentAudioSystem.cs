using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Audio;
using Content.Shared.Audio.Events;
using Content.Shared.GameTicking;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Audio;

public sealed class ContentAudioSystem : SharedContentAudioSystem
{
    /// <summary>
    /// STARLIGHT: Path to the cosmonaut lobby music track used for revolutionary victory
    /// </summary>
    public const string RevVictoryMusic = "/Audio/_Starlight/Music/Lobby/Resistance.ogg";
    
    [ValidatePrototypeId<SoundCollectionPrototype>]
    private const string LobbyMusicCollection = "LobbyMusic";

    [Dependency] private readonly AudioSystem _serverAudio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SoundCollectionPrototype _lobbyMusicCollection = default!;
    private string[]? _lobbyPlaylist;
    
    // STARLIGHT: Flag to indicate if we should use a custom playlist for the next round end
    private bool _useCustomPlaylist;
    private string? _customFirstTrack;

    public override void Initialize()
    {
        base.Initialize();

        _lobbyMusicCollection = _prototypeManager.Index<SoundCollectionPrototype>(LobbyMusicCollection);
        _lobbyPlaylist = ShuffleLobbyPlaylist();

        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        SilenceAudio();
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<AudioPresetPrototype>())
            _serverAudio.ReloadPresets();
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        // On cleanup all entities get purged so need to ensure audio presets are still loaded
        // yeah it's whacky af.
        _serverAudio.ReloadPresets();
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        if (_lobbyPlaylist != null)
        {
            var session = ev.PlayerSession;
            RaiseNetworkEvent(new LobbyPlaylistChangedEvent(_lobbyPlaylist), session);
        }
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // The lobby song is set here instead of in RestartRound,
        // because ShowRoundEndScoreboard triggers the start of the music playing
        // at the end of a round, and this needs to be set before RestartRound
        // in order for the lobby song status display to be accurate.
        
        // STARLIGHT: Check if we should use a custom playlist
        if (_useCustomPlaylist && _customFirstTrack != null)
        {
            _lobbyPlaylist = CreatePlaylistWithFirstTrack(_customFirstTrack);
            // Reset the flag and custom track
            _useCustomPlaylist = false;
            _customFirstTrack = null;
        }
        else
        {
            _lobbyPlaylist = ShuffleLobbyPlaylist();
        }
        
        RaiseNetworkEvent(new LobbyPlaylistChangedEvent(_lobbyPlaylist));
    }

    private string[] ShuffleLobbyPlaylist()
    {
        var playlist = _lobbyMusicCollection.PickFiles
                                            .Select(x => x.ToString())
                                            .ToArray();
         _robustRandom.Shuffle(playlist);

         return playlist;
    }
    
    /// <summary>
    /// STARLIGHT: Creates a playlist with a specific track as the first item, followed by the rest of the tracks shuffled.
    /// </summary>
    /// <param name="firstTrack">The track to place at the beginning of the playlist</param>
    /// <returns>A playlist array with the specified track first</returns>
    public string[] CreatePlaylistWithFirstTrack(string firstTrack)
    {
        // Get all tracks except the one we want first
        var otherTracks = _lobbyMusicCollection.PickFiles
                                              .Select(x => x.ToString())
                                              .Where(x => x != firstTrack)
                                              .ToList();
        
        // Shuffle the other tracks
        _robustRandom.Shuffle(otherTracks);
        
        // Create the final playlist with the specified track first
        var playlist = new List<string> { firstTrack };
        playlist.AddRange(otherTracks);
        
        return playlist.ToArray();
    }
    
    /// <summary>
    /// Sets the lobby playlist with a specific track as the first item and broadcasts it to clients.
    /// </summary>
    /// <param name="firstTrack">The track to place at the beginning of the playlist</param>
    public void SetLobbyPlaylistWithFirstTrack(string firstTrack)
    {
        // Instead of immediately setting the playlist, we'll set a flag to use this track
        // when the round end event is raised
        _useCustomPlaylist = true;
        _customFirstTrack = firstTrack;
    }
}
