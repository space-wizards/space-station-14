using Robust.Shared.Audio;

namespace Content.Shared.Radiation.Components;

[RegisterComponent]
public sealed class RadiationPulseComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("RadiationPulse");

    public TimeSpan StartTime;
    public float VisualDuration = 2f;
    public float VisualRange = 5f;
}
