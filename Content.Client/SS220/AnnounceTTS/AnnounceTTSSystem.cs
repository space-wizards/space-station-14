using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.AnnounceTTS;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client.SS220.AnnounceTTS;

// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSSystem : EntitySystem
{
    [Dependency] private readonly IClydeAudio _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private ISawmill _sawmill = default!;
    private float _volume = 0.0f;

    private  AudioStream? _currentlyPlaying = null;
    private readonly HashSet<AudioStream> _currentStreams = new();
    private readonly Dictionary<int, Queue<AudioStream>> _entityQueues = new();
    private readonly Queue<AudioStream> _queuedStreams = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("AnnounceTTSSystem");
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        SubscribeNetworkEvent<AnnounceTTSEvent>(OnAnnounceTTSPlay);
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        if (_queuedStreams.Count != 0 && (_currentlyPlaying == null || !_currentlyPlaying.Source.IsPlaying))
            ProcessEntityQueue();
    }

    /// <inheritdoc />
    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        EndStreams();
    }

    private void OnAnnounceTTSPlay(AnnounceTTSEvent ev)
    {
        var volume = _volume;
        if (!_resourceCache.TryGetResource<AudioResource>(new ResPath(ev.AnnouncementSound), out var audio))
        {
            _sawmill.Error($"Server tried to play audio file {ev.AnnouncementSound} which does not exist.");
            return;
        }

        if (TryCreateAudioSource(audio, ev.AnnouncementParams.Volume, out var sourceAnnounce))
            AddEntityStreamToQueue(new AudioStream(sourceAnnounce));
        if (ev.Data.Length > 0 && TryCreateAudioSource(ev.Data, volume, out var source))
            AddEntityStreamToQueue(new AudioStream(source, (int)audio.AudioStream.Length.TotalMilliseconds));
    }

    private void AddEntityStreamToQueue(AudioStream stream)
    {
        _queuedStreams.Enqueue(stream);
    }

    private void ProcessEntityQueue()
    {
        if(_queuedStreams.TryDequeue(out _currentlyPlaying))
            PlayEntity(_currentlyPlaying);
    }

    private bool TryCreateAudioSource(byte[] data, float volume, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
        var dataStream = new MemoryStream(data) { Position = 0 };
        var audioStream = _clyde.LoadAudioOggVorbis(dataStream);
        source = _clyde.CreateAudioSource(audioStream);
        source?.SetMaxDistance(float.MaxValue);
        source?.SetReferenceDistance(1f);
        source?.SetRolloffFactor(1f);
        source?.SetVolume(volume);
        return source != null;
    }

    private bool TryCreateAudioSource(AudioResource audio, float volume, [NotNullWhen(true)] out IClydeAudioSource? source)
    {
        source = _clyde.CreateAudioSource(audio);
        source?.SetMaxDistance(float.MaxValue);
        source?.SetReferenceDistance(1f);
        source?.SetRolloffFactor(1f);
        source?.SetVolume(volume);
        return source != null;
    }

    private void PlayEntity(AudioStream stream)
    {
        stream.Source.SetGlobal();
        stream.Source.StartPlaying();
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void EndStreams()
    {
        foreach (var stream in _queuedStreams)
        {
            stream.Source.StopPlaying();
            stream.Source.Dispose();
        }

        _queuedStreams.Clear();
    }

    // ReSharper disable once InconsistentNaming
    private sealed class AudioStream
    {
        public IClydeAudioSource Source { get; }

        public int DelayMs { get; }

        public AudioStream(IClydeAudioSource source, int delayMs = 0)
        {
            Source = source;
            DelayMs = delayMs;
        }
    }
}
