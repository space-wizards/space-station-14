using Content.Shared.DeadSpace.Ports.Jukebox;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.Ports.Jukebox;

public sealed class JukeboxSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAudioManager _clydeAudio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const CollisionGroup CollisionMask = CollisionGroup.Impassable;

    private readonly Dictionary<WhiteJukeboxComponent, JukeboxAudio> _playingJukeboxes = new();

    private const float MinimalVolume = -14f;
    private float _jukeboxVolume;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhiteJukeboxComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
        SubscribeNetworkEvent<JukeboxStopPlaying>(OnStopPlaying);

        _cfg.OnValueChanged(CCCCVars.JukeboxMusicVolume, JukeboxVolumeChanged, true);
    }

    private void JukeboxVolumeChanged(float volume)
    {
        _jukeboxVolume = volume;
        foreach (var jukebox in _playingJukeboxes.Values)
        {
            if (jukebox.PlayingStream.Playing)
            {
                jukebox.PlayingStream.Volume =
                    _jukeboxVolume <= 0f ? float.NegativeInfinity : MinimalVolume + _jukeboxVolume;
            }
        }
    }

    private void JoinLobby(TickerJoinLobbyEvent ev)
    {
        CleanUp();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanUp();
    }

    private void OnComponentRemoved(EntityUid uid, WhiteJukeboxComponent component, ComponentRemove args)
    {
        if (!_playingJukeboxes.TryGetValue(component, out var playingData)) return;

        playingData.PlayingStream.StopPlaying();
        _playingJukeboxes.Remove(component);
    }

    private void OnStopPlaying(JukeboxStopPlaying ev)
    {
        if (!ev.JukeboxUid.HasValue) return;
        if (!TryComp<WhiteJukeboxComponent>(GetEntity(ev.JukeboxUid), out var jukeboxComponent)) return;

        if (!_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio)) return;

        jukeboxAudio.PlayingStream.StopPlaying();
        _playingJukeboxes.Remove(jukeboxComponent);
    }

    public void RequestSongToPlay(EntityUid jukebox, WhiteJukeboxComponent component, JukeboxSong jukeboxSong)
    {
        if (!_resource.TryGetResource<AudioResource>((ResPath) jukeboxSong.SongPath!, out var songResource))
        {
            return;
        }

        RaiseNetworkEvent(new JukeboxRequestSongPlay
        {
            Jukebox = GetNetEntity(jukebox),
            SongName = jukeboxSong.SongName,
            SongPath = jukeboxSong.SongPath,
            SongDuration = (float) songResource.AudioStream.Length.TotalSeconds
        });
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var localPlayerEntity = _playerManager.LocalEntity;
        if (!localPlayerEntity.HasValue)
        {
            CleanUp();
            return;
        }

        ProcessJukeboxes();
    }

    private void ProcessJukeboxes()
    {
        var jukeboxes = EntityQueryEnumerator<WhiteJukeboxComponent, TransformComponent>();
        var player = _playerManager.LocalEntity!.Value;
        var playerXform = Comp<TransformComponent>(player);

        while (jukeboxes.MoveNext(out var jukebox, out var jukeboxComponent, out var jukeboxXform))
        {
            if (jukeboxXform.MapID != playerXform.MapID ||
                (_transform.GetWorldPosition(jukebox) - _transform.GetWorldPosition(player)).Length() >
                jukeboxComponent.MaxAudioRange)
            {
                if (_playingJukeboxes.Remove(jukeboxComponent, out var stream))
                {
                    stream.PlayingStream.StopPlaying();
                    stream.PlayingStream.Dispose();
                }

                continue;
            }

            if (_playingJukeboxes.TryGetValue(jukeboxComponent, out var jukeboxAudio))
            {
                if (!jukeboxAudio.PlayingStream.Playing)
                {
                    HandleDoneStream(jukebox, player, jukeboxAudio, jukeboxComponent);
                    continue;
                }

                if (jukeboxAudio.SongData.SongPath != jukeboxComponent.PlayingSongData?.SongPath)
                {
                    HandleSongChanged(jukebox, player, jukeboxAudio, jukeboxComponent);
                    continue;
                }

                SetRolloffAndOcclusion(jukebox, player, jukeboxComponent, jukeboxAudio);
                SetPosition(jukebox, jukeboxAudio);
            }
            else
            {
                if (jukeboxComponent.PlayingSongData == null)
                {
                    SetBarsLayerVisible(jukebox, false);
                    continue;
                }

                var stream = TryCreateStream(jukebox, player, jukeboxComponent);

                if (stream == null)
                {
                    continue;
                }

                _playingJukeboxes.Add(jukeboxComponent, stream);
                SetBarsLayerVisible(jukebox, true);
            }
        }
    }

    private void SetPosition(EntityUid jukebox, JukeboxAudio jukeboxAudio)
    {
        jukeboxAudio.PlayingStream.Position = _transform.GetWorldPosition(jukebox);
    }

    private void SetRolloffAndOcclusion(
        EntityUid player,
        EntityUid jukebox,
        WhiteJukeboxComponent jukeboxComponent,
        JukeboxAudio jukeboxAudio)
    {
        var jukeboxWorldPosition = _transform.GetWorldPosition(jukebox);
        var playerWorldPosition = _transform.GetWorldPosition(player);
        var sourceRelative = playerWorldPosition - jukeboxWorldPosition;
        var occlusion = 0f;

        if (sourceRelative.Length() > 0)
        {
            occlusion = _physicsSystem.IntersectRayPenetration(_transform.GetMapCoordinates(jukebox).MapId,
                new CollisionRay(jukeboxWorldPosition, sourceRelative.Normalized(), (int) CollisionMask),
                sourceRelative.Length(), jukebox) * 3f;
        }

        jukeboxAudio.PlayingStream.Occlusion = occlusion;
        jukeboxAudio.PlayingStream.RolloffFactor =
            (jukeboxWorldPosition - playerWorldPosition).Length() * jukeboxComponent.RolloffFactor;
    }

    private void HandleSongChanged(
        EntityUid jukebox,
        EntityUid player,
        JukeboxAudio jukeboxAudio,
        WhiteJukeboxComponent jukeboxComponent)
    {
        jukeboxAudio.PlayingStream.StopPlaying();

        if (jukeboxComponent.PlayingSongData != null &&
            jukeboxComponent.PlayingSongData.SongPath == jukeboxAudio.SongData.SongPath)
        {
            var newStream = TryCreateStream(jukebox, player, jukeboxComponent);
            if (newStream == null) return;

            _playingJukeboxes[jukeboxComponent] = newStream;
            SetBarsLayerVisible(jukebox, true);
        }
        else
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
        }
    }

    private void HandleDoneStream(
        EntityUid jukebox,
        EntityUid player,
        JukeboxAudio jukeboxAudio,
        WhiteJukeboxComponent jukeboxComponent)
    {
        if (!jukeboxComponent.Playing)
        {
            jukeboxAudio.PlayingStream.StopPlaying();
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
            return;
        }

        if (jukeboxComponent.PlayingSongData == null) return;

        var newStream = TryCreateStream(jukebox, player, jukeboxComponent);

        if (newStream == null)
        {
            _playingJukeboxes.Remove(jukeboxComponent);
            SetBarsLayerVisible(jukebox, false);
        }
        else
        {
            _playingJukeboxes[jukeboxComponent] = newStream;
            SetBarsLayerVisible(jukebox, true);
        }
    }

    private JukeboxAudio? TryCreateStream(EntityUid jukebox, EntityUid player, WhiteJukeboxComponent jukeboxComponent)
    {
        if (jukeboxComponent.PlayingSongData == null) return null!;

        var resourcePath = jukeboxComponent.PlayingSongData.SongPath!;

        if (!_resource.TryGetResource<AudioResource>((ResPath) resourcePath, out var audio))
            return null;

        if (audio.AudioStream.Length.TotalSeconds < jukeboxComponent.PlayingSongData!.PlaybackPosition)
        {
            return null;
        }

        var playingStream = _clydeAudio.CreateAudioSource(audio.AudioStream);

        if (playingStream == null)
            return null;

        playingStream.Volume = _jukeboxVolume <= 0f ? float.NegativeInfinity : MinimalVolume + _jukeboxVolume;
        playingStream.PlaybackPosition = jukeboxComponent.PlayingSongData.PlaybackPosition;

        playingStream.Position = _transform.GetWorldPosition(jukebox);

        var jukeboxAudio = new JukeboxAudio(playingStream, audio, jukeboxComponent.PlayingSongData);

        SetRolloffAndOcclusion(jukebox, player, jukeboxComponent, jukeboxAudio);
        playingStream.StartPlaying();

        return jukeboxAudio;
    }

    private void SetBarsLayerVisible(EntityUid jukebox, bool visible)
    {
        var spriteComponent = Comp<SpriteComponent>(jukebox);
        spriteComponent.LayerMapTryGet("bars", out var layer);
        spriteComponent.LayerSetVisible(layer, visible);
    }

    private sealed class JukeboxAudio(IAudioSource playingStream, AudioResource audioStream, PlayingSongData songData)
    {
        public PlayingSongData SongData { get; } = songData;

        public IAudioSource PlayingStream { get; } = playingStream;

        public AudioResource AudioStream { get; } = audioStream;
    }

    private void CleanUp()
    {
        foreach (var playingJukebox in _playingJukeboxes.Values)
        {
            playingJukebox.PlayingStream.StopPlaying();
            playingJukebox.PlayingStream.Dispose();
        }

        _playingJukeboxes.Clear();
    }
}
