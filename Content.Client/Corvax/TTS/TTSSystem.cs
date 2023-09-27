using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.TTS.Commands;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private ISawmill _sawmill = default!;

    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;
    private int _fileIdx = 0;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _resourceCache.AddRoot(Prefix, _contentRoot);
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
        _contentRoot.Dispose();
    }

    public void RequestGlobalTTS(string text, string voiceId)
    {
        RaiseNetworkEvent(new RequestGlobalTTSEvent(text, voiceId));
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
        //EndStreams();
        _sawmill.Debug("TTS queue was cleared by request from the server.");
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Debug($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var volume = (ev.IsRadio ? _radioVolume : _volume) * ev.VolumeModifier;

        var filePath = new ResPath($"{_fileIdx++}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioParams = AudioParams.Default.WithVolume(volume);
        var soundPath = new SoundPathSpecifier(Prefix / filePath, audioParams);
        if (ev.SourceUid != null)
        {
            var sourceUid = GetEntity(ev.SourceUid.Value);
            _audio.PlayEntity(soundPath, new EntityUid(), sourceUid); // recipient arg ignored on client
        }
        else
        {
            _audio.PlayGlobal(soundPath, Filter.Local(), false);
        }

        _contentRoot.RemoveFile(filePath);
    }
}
