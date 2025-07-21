using System.Collections.Concurrent;
using System.IO;
using Content.Client._Starlight.Radio.Systems;
using Content.Shared.Starlight.CCVar;
using Content.Shared.Starlight.TextToSpeech;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Starlight.TTS;

/// <summary>
/// Plays TTS audio
/// </summary>
public sealed class TextToSpeechSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RadioChimeSystem _chime = default!;

    private readonly ConcurrentQueue<(byte[] file, SoundSpecifier? specifier, float volume)> _ttsQueue = [];
    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private (EntityUid Entity, AudioComponent Component)? _currentPlaying;

    private float _volume;
    private float _radioVolume;
    private float _announceVolume;
    private bool _ttsQueueEnabled;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(StarlightCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSRadioQueueEnabled, OnTtsRadioQueueChanged, true);
        _cfg.OnValueChanged(StarlightCCVars.TTSClientEnabled, OnTtsClientOptionChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<AnnounceTtsEvent>(OnAnnounceTTSPlay);
        SubscribeLocalEvent<ClientTTSAudioComponent, ComponentRemove>(OnClientTTSAudioRemove);
        SubscribeLocalEvent<ClientTTSAudioComponent, EntityTerminatingEvent>(OnClientTTSAudioRemove);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(StarlightCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSRadioQueueEnabled, OnTtsRadioQueueChanged);
        _cfg.UnsubValueChanged(StarlightCCVars.TTSClientEnabled, OnTtsClientOptionChanged);
        _contentRoot.Dispose();
    }

    public void RequestPreviewTts(string voiceId)
        => RaiseNetworkEvent(new PreviewTTSRequestEvent() { VoiceId = voiceId });

    private void OnTtsVolumeChanged(float volume)
        => _volume = volume;

    private void OnTtsRadioVolumeChanged(float volume)
        => _radioVolume = volume;

    private void OnTtsRadioQueueChanged(bool enabled)
        => _ttsQueueEnabled = enabled;

    private void OnTtsAnnounceVolumeChanged(float volume)
        => _announceVolume = volume;

    private void OnTtsClientOptionChanged(bool option)
        => RaiseNetworkEvent(new ClientOptionTTSEvent { Enabled = option });

    private void OnAnnounceTTSPlay(AnnounceTtsEvent ev)
        => _ttsQueue.Enqueue((ev.Data, ev.AnnouncementSound, _announceVolume));

    private void OnClientTTSAudioRemove<T>(Entity<ClientTTSAudioComponent> ent, ref T args)
    {
        if (ent.Comp.Stream is not { } stream)
            return;

        try
        {
            // Setting capacity to 0 makes MemoryStream drop the reference to its buffer (byte array),
            // letting it be garbage collected
            // Dispose does not do this, at least as of time of writing
            stream.Capacity = 0;

            // We also dispose it since we don't want it to be reused after the data is dropped
            stream.Dispose();
        }
        catch
        {
            // ignored, stream might be closed but we don't care as long as the data goes away
        }
    }

    private void PlayQueue()
    {
        if (!_ttsQueue.TryDequeue(out var entry))
            return;

        var volume = SharedAudioSystem.GainToVolume(entry.volume);
        var finalParams = AudioParams.Default.WithVolume(volume);

        if (entry.specifier != null)
            _currentPlaying = _audio.PlayGlobal(_sharedAudio.ResolveSound(entry.specifier), EntityUid.Invalid, finalParams.AddVolume(-5f));
        _currentPlaying = PlayTTSBytes(entry.file, null, finalParams, true);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = ev.IsRadio ? _radioVolume : _volume;

        if (ev.IsRadio && _ttsQueueEnabled)
        {
            _ttsQueue.Enqueue((ev.Data, !_chime.IsMuted ? ev.Chime : null, _radioVolume));
        }
        else
        {
            volume = SharedAudioSystem.GainToVolume(volume * ev.VolumeModifier);
            var audioParams = AudioParams.Default.WithVolume(volume);
            var entity = GetEntity(ev.SourceUid);

            if (!_chime.IsMuted && ev.Chime is SoundSpecifier chime)
                _currentPlaying = _audio.PlayGlobal(_sharedAudio.ResolveSound(chime), EntityUid.Invalid, audioParams.AddVolume(-3f));

            PlayTTSBytes(ev.Data, entity, audioParams);
        }
    }

    private (EntityUid Entity, AudioComponent Component)? PlayTTSBytes(byte[] data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        if (data.Length < 50 || sourceUid != null && sourceUid.Value.Id == 0 && !globally)
            return null;

        _sawmill.Debug($"Play TTS audio {data.Length} bytes");

        var @params = audioParams ?? AudioParams.Default;
        using var stream = new MemoryStream(data);
        var audioStream = _audioManager.LoadAudioOggVorbis(stream);

        var ent = globally
            ? _audio.PlayGlobal(audioStream, null, @params)
            : sourceUid != null
                ? _audio.PlayEntity(audioStream, sourceUid.Value, null, @params)
                : _audio.PlayGlobal(audioStream, null, @params);

        if (ent != null)
            EnsureComp<ClientTTSAudioComponent>(ent.Value.Entity).Stream = stream;

        return ent;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_currentPlaying.HasValue)
        {
            var (entity, _) = _currentPlaying.Value;

            if (Deleted(entity))
                _currentPlaying = null;
            else
                return;
        }

        PlayQueue();
    }
}
