using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Extinguisher;

public abstract class SharedFireExtinguisherComponent : Component
{
    [DataField("refillSound")] public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField("hasSafety")] public bool HasSafety = true;

    [DataField("safety")] public bool Safety = true;

    /// <summary>
    ///     Reagent that will be used as cooler for extinguisher.
    /// </summary>
    [DataField("waterReagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string WaterReagent { get; } = "Water";

    [DataField("safetySound")]
    public SoundSpecifier SafetySound { get; } = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}


[Serializable, NetSerializable]
public enum FireExtinguisherVisuals : byte
{
    Safety
}
