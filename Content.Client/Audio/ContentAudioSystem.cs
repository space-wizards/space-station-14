using Content.Shared.Audio;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Shared;
using Robust.Shared.Audio;
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
        var change = (curVolume - MinVolume) / duration;
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
            component.Volume = MathF.Max(MinVolume, volume);

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

            var volume = component.Volume + change * frameTime;
            component.Volume = MathF.Min(target, volume);

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
