using Content.Shared.Audio;
using Content.Shared.GameTicking;
using AudioComponent = Robust.Shared.Audio.Components.AudioComponent;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem : SharedContentAudioSystem
{
    // Need how much volume to change per tick and just remove it when it drops below "0"
    private readonly Dictionary<EntityUid, float> _fadingOut = new();

    // Need volume change per tick + target volume.
    private readonly Dictionary<EntityUid, (float VolumeChange, float TargetVolume)> _fadingIn = new();

    private readonly List<EntityUid> _fadeToRemove = new();

    private const float MinVolume = -32f;
    private const float DefaultDuration = 2f;

    /*
     * Gain multipliers for specific audio sliders.
     * The float value will get multiplied by this when setting
     * i.e. a gain of 0.5f x 3 will equal 1.5f which is supported in OpenAL.
     */

    public const float MasterVolumeMultiplier = 3f;
    public const float MidiVolumeMultiplier = 0.25f;
    public const float AmbienceMultiplier = 3f;
    public const float AmbientMusicMultiplier = 3f;
    public const float LobbyMultiplier = 3f;
    public const float InterfaceMultiplier = 2f;
    
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
        InitializeAmbientMusic();
        InitializeLobbyMusic();
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        _fadingOut.Clear();

        // Preserve lobby music but everything else should get dumped.
        var lobbyMusic = _lobbySoundtrackInfo?.MusicStreamEntityUid;
        TryComp(lobbyMusic, out AudioComponent? lobbyMusicComp);
        var oldMusicGain = lobbyMusicComp?.Gain;

        var restartAudio = _lobbyRoundRestartAudioStream;
        TryComp(restartAudio, out AudioComponent? restartComp);
        var oldAudioGain = restartComp?.Gain;

        SilenceAudio();

        if (oldMusicGain != null)
        {
            Audio.SetGain(lobbyMusic, oldMusicGain.Value, lobbyMusicComp);
        }

        if (oldAudioGain != null)
        {
            Audio.SetGain(restartAudio, oldAudioGain.Value, restartComp);
        }
        PlayRestartSound(ev);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownAmbientMusic();
        ShutdownLobbyMusic();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        UpdateAmbientMusic();
        UpdateLobbyMusic();
        UpdateFades(frameTime);
    }

    #region Fades

    public void FadeOut(EntityUid? stream, AudioComponent? component = null, float duration = DefaultDuration)
    {
        if (stream == null || duration <= 0f || !Resolve(stream.Value, ref component))
            return;

        // Just in case
        // TODO: Maybe handle the removals by making it seamless?
        _fadingIn.Remove(stream.Value);
        var diff = component.Volume - MinVolume;
        _fadingOut.Add(stream.Value, diff / duration);
    }

    public void FadeIn(EntityUid? stream, AudioComponent? component = null, float duration = DefaultDuration)
    {
        if (stream == null || duration <= 0f || !Resolve(stream.Value, ref component) || component.Volume < MinVolume)
            return;

        _fadingOut.Remove(stream.Value);
        var curVolume = component.Volume;
        var change = (MinVolume - curVolume) / duration;
        _fadingIn.Add(stream.Value, (change, component.Volume));
        component.Volume = MinVolume;
    }

    private void UpdateFades(float frameTime)
    {
        _fadeToRemove.Clear();

        foreach (var (stream, change) in _fadingOut)
        {
            if (!TryComp(stream, out AudioComponent? component))
            {
                _fadeToRemove.Add(stream);
                continue;
            }

            var volume = component.Volume - change * frameTime;
            volume = MathF.Max(MinVolume, volume);
            _audio.SetVolume(stream, volume, component);

            if (component.Volume.Equals(MinVolume))
            {
                _audio.Stop(stream);
                _fadeToRemove.Add(stream);
            }
        }

        foreach (var stream in _fadeToRemove)
        {
            _fadingOut.Remove(stream);
        }

        _fadeToRemove.Clear();

        foreach (var (stream, (change, target)) in _fadingIn)
        {
            // Cancelled elsewhere
            if (!TryComp(stream, out AudioComponent? component))
            {
                _fadeToRemove.Add(stream);
                continue;
            }

            var volume = component.Volume - change * frameTime;
            volume = MathF.Min(target, volume);
            _audio.SetVolume(stream, volume, component);

            if (component.Volume.Equals(target))
            {
                _fadeToRemove.Add(stream);
            }
        }

        foreach (var stream in _fadeToRemove)
        {
            _fadingIn.Remove(stream);
        }
    }

    #endregion
}

/// <summary>
/// Raised whenever ambient music tries to play.
/// </summary>
[ByRefEvent]
public record struct PlayAmbientMusicEvent(bool Cancelled = false);
