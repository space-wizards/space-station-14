using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Extinguisher;

public abstract partial class SharedFireExtinguisherComponent : Component
{
    [DataField("refillSound")] public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField("hasSafety")] public bool HasSafety = true;

    [DataField("safety")] public bool Safety = true;

    [DataField("safetySound")]
    public SoundSpecifier SafetySound { get; private set; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}


[Serializable, NetSerializable]
public enum FireExtinguisherVisuals : byte
{
    Safety
}
