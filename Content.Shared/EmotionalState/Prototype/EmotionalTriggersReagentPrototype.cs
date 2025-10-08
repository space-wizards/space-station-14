using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.EmotionalState;

[Prototype("emotionalTriggersReagent")]
public sealed partial class EmotionalTriggersReagentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<EmotionalTriggersReagentPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The prototype of a substance that is considered a trigger.
    /// </summary>
    [DataField("triggersPrototype")]
    public string TriggersPrototype { get; private set; } = "Ethanol";

    /// <summary>
    /// How many emotional state points the humanoid will receive when this trigger is present in their organism.
    /// The positive effect is INSTANTANEOUS! This represents a kind of dependency on certain
    /// substances contained, for example, in chocolate.
    /// </summary>
    [DataField("positEffect")]
    public float PositEffect { get; private set; } = 0f; // The positive effect will be instantaneous.

    /// <summary>
    /// How many emotional state points the humanoid will lose when this trigger is present in their organism.
    /// Everything is calculated in the <see cref="EmotionalStateSystem.UpdateCurrentThreshold()"/> method.
    /// There, the amount of the substance (in ounces) that will be metabolized this tick is calculated and multiplied by this field's value,
    /// to determine how many points to subtract due to the substance in the organism.
    /// </summary>
    [DataField("negatEffect")]
    public float NegatEffect { get; private set; } = 0f; // The negative effect will last for a period of time (0.25f every second)

    /// <summary>
    /// The duration for which the negative effect will be active.
    /// </summary>
    [DataField("negatEffectTime")]
    public int NegatEffectTime { get; private set; } = 0;

    /// <summary>
    /// The race for which this trigger will be effective.
    /// </summary>
    [DataField("species")]
    public List<String> Species { get; private set; } = new List<String>();
}
