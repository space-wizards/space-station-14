using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Extinguisher;

public abstract class SharedFireExtinguisherComponent : Component
{
    [DataField("refillSound")] public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField("hasSafety")] public bool HasSafety = true;

    [DataField("safety")] public bool Safety = true;

    [DataField("safetySound")]
    public SoundSpecifier SafetySound { get; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}


[Serializable, NetSerializable]
public enum FireExtinguisherVisuals : byte
{
    Safety
}
