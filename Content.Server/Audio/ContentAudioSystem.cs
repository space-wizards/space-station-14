using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Audio;
using Content.Shared.Audio.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


namespace Content.Server.Audio;

public sealed class ContentAudioSystem : SharedContentAudioSystem
{
    [Dependency] private readonly AudioSystem _serverAudio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private SoundCollectionPrototype? _lobbyMusicCollection = default!;
    private string[]? _lobbyPlaylist;

    public override void Initialize()
    {
        base.Initialize();

        //changes the music collection and reshuffles the playlist to update the lobby music
        Subs.CVar(
            _cfg,
            CCVars.LobbyMusicCollection,
            x =>
            {
                //Checks to see if the sound collection exists. If it does change it if not defaults to null
                // as the new _lobbyMusicCollection meaning it wont play anything in the lobby.
                if(_prototypeManager.TryIndex<SoundCollectionPrototype>(x, out var outputSoundCollection))
                {
                    _lobbyMusicCollection = outputSoundCollection;
                }
                else
                {
                    Log.Error($"Invalid Lobby Music sound collection specified: {x}");
                    _lobbyMusicCollection = null;
                }

                _lobbyPlaylist = ShuffleLobbyPlaylist();
            },
            true);

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
        _lobbyPlaylist = ShuffleLobbyPlaylist();
        RaiseNetworkEvent(new LobbyPlaylistChangedEvent(_lobbyPlaylist));
    }

    private string[] ShuffleLobbyPlaylist()
    {
        if (_lobbyMusicCollection == null)
        {
            return [];
        }

        var playlist = _lobbyMusicCollection.PickFiles
                                            .Select(x => x.ToString())
                                            .ToArray();
        _robustRandom.Shuffle(playlist);

        return playlist;
    }
}
