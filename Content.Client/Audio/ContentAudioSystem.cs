using Content.Shared.Audio;
using Robust.Client.GameObjects;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem : SharedContentAudioSystem
{
    // Need how much volume to change per tick and just remove it when it drops below "0"
    private readonly Dictionary<AudioSystem.PlayingStream, float> _fadingOut = new();

    // Need volume change per tick + target volume.
    private readonly Dictionary<AudioSystem.PlayingStream, (float VolumeChange, float TargetVolume)> _fadingIn = new();

    private readonly List<AudioSystem.PlayingStream> _fadeToRemove = new();

    private const float MinVolume = -32f;
    private const float DefaultDuration = 2f;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        InitializeAmbientMusic();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownAmbientMusic();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        UpdateAmbientMusic();
        UpdateFades(frameTime);
    }

    #region Fades

    public void FadeOut(AudioSystem.PlayingStream? stream, float duration = DefaultDuration)
    {
        if (stream == null || duration <= 0f)
            return;

        // Just in case
        // TODO: Maybe handle the removals by making it seamless?
        _fadingIn.Remove(stream);
        var diff = stream.Volume - MinVolume;
        _fadingOut.Add(stream, diff / duration);
    }

    public void FadeIn(AudioSystem.PlayingStream? stream, float duration = DefaultDuration)
    {
        if (stream == null || duration <= 0f || stream.Volume < MinVolume)
            return;

        _fadingOut.Remove(stream);
        var curVolume = stream.Volume;
        var change = (curVolume - MinVolume) / duration;
        _fadingIn.Add(stream, (change, stream.Volume));
        stream.Volume = MinVolume;
    }

    private void UpdateFades(float frameTime)
    {
        _fadeToRemove.Clear();

        foreach (var (stream, change) in _fadingOut)
        {
            // Cancelled elsewhere
            if (stream.Done)
            {
                _fadeToRemove.Add(stream);
                continue;
            }

            var volume = stream.Volume - change * frameTime;
            stream.Volume = MathF.Max(MinVolume, volume);

            if (stream.Volume.Equals(MinVolume))
            {
                stream.Stop();
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
            if (stream.Done)
            {
                _fadeToRemove.Add(stream);
                continue;
            }

            var volume = stream.Volume + change * frameTime;
            stream.Volume = MathF.Min(target, volume);

            if (stream.Volume.Equals(target))
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
