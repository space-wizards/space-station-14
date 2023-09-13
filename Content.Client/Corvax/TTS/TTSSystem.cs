using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.TTS.Commands;
using Content.Shared.Physics;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IClydeAudio _clyde = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPhysicsSystem _broadPhase = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;

    private readonly HashSet<AudioStream> _currentStreams = new();
    private readonly Dictionary<EntityUid, Queue<AudioStream>> _entityQueues = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<TtsQueueResetMessage>(OnQueueResetRequest);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        EndStreams();
    }

    // Little bit of duplication logic from AudioSystem
    public override void FrameUpdate(float frameTime)
    {
        var streamToRemove = new HashSet<AudioStream>();

        var ourPos = _eye.CurrentEye.Position.Position;
        foreach (var stream in _currentStreams)
        {
            var streamUid = GetEntity(stream.Uid);
            if (!stream.Source.IsPlaying ||
                !_entity.TryGetComponent<MetaDataComponent>(streamUid, out var meta) ||
                Deleted(streamUid, meta) ||
                !_entity.TryGetComponent<TransformComponent>(streamUid, out var xform))
            {
                stream.Source.Dispose();
                streamToRemove.Add(stream);
                continue;
            }

            var mapPos = xform.MapPosition;
            if (mapPos.MapId != MapId.Nullspace)
            {
                if (!stream.Source.SetPosition(mapPos.Position))
                {
                    _sawmill.Warning("Can't set position for audio stream, stop stream.");
                    stream.Source.StopPlaying();
                }
            }

            if (mapPos.MapId == _eye.CurrentMap)
            {
                var collisionMask = (int) CollisionGroup.Impassable;
                var sourceRelative = ourPos - mapPos.Position;
                var occlusion = 0f;
                if (sourceRelative.Length() > 0)
                {
                    occlusion = _broadPhase.IntersectRayPenetration(mapPos.MapId,
                        new CollisionRay(mapPos.Position, sourceRelative.Normalized(), collisionMask),
                        sourceRelative.Length(), streamUid);
                }
                stream.Source.SetOcclusion(occlusion);
            }
        }

        foreach (var audioStream in streamToRemove)
        {
            _currentStreams.Remove(audioStream);
            ProcessEntityQueue(GetEntity(audioStream.Uid));
        }
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnQueueResetRequest(TtsQueueResetMessage ev)
    {
        EndStreams();
        _sawmill.Debug("TTS queue was cleared by request from the server.");
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = (ev.IsRadio ? _radioVolume : _volume) * ev.VolumeModifier;

        if (!TryCreateAudioSource(ev.Data, volume, out var source))
            return;

        var stream = new AudioStream(ev.Uid, source);
        AddEntityStreamToQueue(stream);
    }

    public void StopAllStreams()
    {
        foreach (var stream in _currentStreams)
            stream.Source.StopPlaying();
    }

    private bool TryCreateAudioSource(byte[] data, float volume, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
        var dataStream = new MemoryStream(data) { Position = 0 };
        var audioStream = _clyde.LoadAudioOggVorbis(dataStream);
        source = _clyde.CreateAudioSource(audioStream);
        source?.SetVolume(volume);
        return source != null;
    }

    private void AddEntityStreamToQueue(AudioStream stream)
    {
        var uid = GetEntity(stream.Uid);
        if (_entityQueues.TryGetValue(uid, out var queue))
        {
            queue.Enqueue(stream);
        }
        else
        {
            _entityQueues.Add(uid, new Queue<AudioStream>(new[] { stream }));

            if (!IsEntityCurrentlyPlayStream(stream.Uid))
                ProcessEntityQueue(uid);
        }
    }

    private bool IsEntityCurrentlyPlayStream(NetEntity uid)
    {
        return _currentStreams.Any(s => s.Uid == uid);
    }

    private void ProcessEntityQueue(EntityUid uid)
    {
        if (TryTakeEntityStreamFromQueue(uid, out var stream))
            PlayEntity(stream);
    }

    private bool TryTakeEntityStreamFromQueue(EntityUid uid, [NotNullWhen(true)] out AudioStream? stream)
    {
        if (_entityQueues.TryGetValue(uid, out var queue))
        {
            stream = queue.Dequeue();
            if (queue.Count == 0)
                _entityQueues.Remove(uid);
            return true;
        }

        stream = null;
        return false;
    }

    private void PlayEntity(AudioStream stream)
    {
        if (!_entity.TryGetComponent<TransformComponent>(GetEntity(stream.Uid), out var xform) ||
            !stream.Source.SetPosition(_transform.GetWorldPosition(xform)))
            return;

        stream.Source.StartPlaying();
        _currentStreams.Add(stream);
    }

    public void EndStreams()
    {
        foreach (var stream in _currentStreams)
        {
            stream.Source.StopPlaying();
            stream.Source.Dispose();
        }

        _currentStreams.Clear();
        _entityQueues.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class AudioStream
    {
        public NetEntity Uid { get; }
        public IClydeAudioSource Source { get; }

        public AudioStream(NetEntity uid, IClydeAudioSource source)
        {
            Uid = uid;
            Source = source;
        }
    }
}
