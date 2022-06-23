using Content.Shared.Sound;

namespace Content.Server.Extinguisher;

[RegisterComponent]
[Access(typeof(FireExtinguisherSystem))]
public sealed class FireExtinguisherComponent : Component
{
    [DataField("refillSound")] public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField("hasSafety")] public bool HasSafety = true;

    [DataField("safety")] public bool Safety = true;

    [DataField("safetySound")]
    public SoundSpecifier SafetySound { get; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
