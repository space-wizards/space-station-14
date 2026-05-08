using Robust.Shared.Audio;

namespace Content.Shared.Shadowkin;

[RegisterComponent]
public sealed partial class ShadowkinLightDampeningComponent : Component
{
    [DataField]
    public float Radius = 4f;

    [DataField]
    public float Duration = 5f;

    [DataField]
    public float FlickerStartDelay = 0.25f;

    [DataField]
    public float BlackoutDuration = 10f;

    [DataField]
    public SoundSpecifier? FlickerSound = new SoundPathSpecifier(
        "/Audio/Machines/light_tube_on.ogg",
        AudioParams.Default.WithVolume(-8f).WithMaxDistance(8f));

    [DataField]
    public int MaxSoundsPerStep = 2;

    public TimeSpan StartTime;
    public TimeSpan EndTime;
    public int NextFlickerStep;
    public bool BlackoutApplied;
    public bool Applied;
    public readonly List<ShadowkinLightState> AffectedLights = new();
}

public readonly record struct ShadowkinLightState(EntityUid Entity, bool WasEnabled, bool IsPowered);
