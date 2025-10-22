using Content.Shared.Chat;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.DeadSpace.CCCCVars;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Content.Server.DeadSpace.Languages;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LanguageSystem _languageSystem = default!;

    private ISawmill _sawmill = default!;
    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 3f;

    /// <summary>
    /// The volume at which the TTS sound will not be heard.
    /// </summary>
    private const float MinimalVolume = -6f;

    private float _volume = 0.0f;
    private float _volumeRadio = 0.0f;
    private bool _playRadio = true;
    private int _fileIdx = 0;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _res.AddRoot(Prefix, _contentRoot);
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCCVars.TTSVolumeRadio, OnTtsRadioVolumeChanged, true);
        _cfg.OnValueChanged(CCCCVars.RadioTTSSoundsEnabled, OnTtsPlayRadioChanged, true);
        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCCVars.TTSVolumeRadio, OnTtsRadioVolumeChanged);
        _cfg.UnsubValueChanged(CCCCVars.RadioTTSSoundsEnabled, OnTtsPlayRadioChanged);
        _contentRoot.Dispose();
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _volumeRadio = volume;
    }
    private void OnTtsPlayRadioChanged(bool radio)
    {
        _playRadio = radio;
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        if (ev.IsRadio && !_playRadio)
            return;

        var hasData = ev.Data is { Length: > 0 };
        var hasLexiconSound = ev.IsLexiconSound && !string.IsNullOrEmpty(ev.LanguageId);

        // Проверяем, что хотя бы один источник звука доступен
        if (!hasData && !hasLexiconSound)
        {
            _sawmill.Warning("Не содержит звуковых данных и допустимого звучания лексики (TTS event has no audio data and no valid lexicon sound)");
            return;
        }

        string verboseMessage;

        if (hasData)
            verboseMessage = $"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity";
        else if (hasLexiconSound)
            verboseMessage = $"Play Lexicon sound '{ev.LanguageId}' from {ev.SourceUid} entity";
        else
            verboseMessage = "Play TTS event with no audio data";

        _sawmill.Verbose(verboseMessage);

        ResolvedPathSpecifier? soundSpecifier = null;
        AudioResource? audioResource = null;
        ResPath? filePath = null;

        var audioParams = AudioParams.Default
            .WithVolume(AdjustVolume(ev.IsWhisper, ev.IsRadio))
            .WithMaxDistance(AdjustDistance(ev.IsWhisper));

        // Если есть обычные данные TTS — готовим ресурс
        if (hasData)
        {
            filePath = new ResPath($"{_fileIdx++}.ogg");
            _contentRoot.AddOrUpdateFile(filePath.Value, ev.Data);

            audioResource = new AudioResource();
            audioResource.Load(IoCManager.Instance!, Prefix / filePath.Value);
            soundSpecifier = new ResolvedPathSpecifier(Prefix / filePath.Value);
        }

        if (ev.SourceUid != null)
        {
            if (!TryGetEntity(ev.SourceUid.Value, out _))
                return;

            var sourceUid = GetEntity(ev.SourceUid.Value);

            if (ev.IsLexiconSound && !string.IsNullOrEmpty(ev.LanguageId))
                _languageSystem.PlayEntityLexiconSound(audioParams, sourceUid, ev.LanguageId);
            else
                _audio.PlayEntity(audioResource!.AudioStream, sourceUid, soundSpecifier, audioParams);
        }
        else
        {
            if (ev.IsLexiconSound && !string.IsNullOrEmpty(ev.LanguageId))
                _languageSystem.PlayGlobalLexiconSound(audioParams, ev.LanguageId);
            else
                _audio.PlayGlobal(audioResource!.AudioStream, soundSpecifier, audioParams);
        }

        if (filePath is not null)
            _contentRoot.RemoveFile(filePath.Value);
    }

    private float AdjustVolume(bool isWhisper, bool isRadio)
    {
        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volume);

        if (isWhisper && !isRadio)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }
        else if (isRadio)
        {
            volume = MinimalVolume + SharedAudioSystem.GainToVolume(_volumeRadio);
        }

        return volume;
    }

    private float AdjustDistance(bool isWhisper)
    {
        return isWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange;
    }
}
