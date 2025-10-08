using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;
using Content.Shared.Roles;

namespace Content.Shared.EmotionalState;

[Prototype("emotionalTriggers")]
public sealed partial class EmotionalTriggersPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<EmotionalTriggersPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The prototype of an entity that is considered a trigger.
    /// </summary>
    [DataField("triggersPrototype")]
    public string TriggersPrototype { get; private set; } = "MobVulpkanin"; // Pls use Mob Prototype. Not Species prototype!

    /// <summary>
    /// The prototype of an entity that is considered a trigger.
    /// </summary>
    [DataField("requiredTrigger")]
    public bool RequiredTrigger { get; private set; } = false;

    /// <summary>
    /// How many emotional state points the humanoid will receive upon seeing this trigger.
    /// </summary>
    [DataField("positEffect")]
    public float PositEffect { get; private set; } = 0f;

    /// <summary>
    /// How many emotional state points the humanoid will lose upon seeing this trigger.
    /// </summary>
    [DataField("negatEffect")]
    public float NegatEffect { get; private set; } = 0f;

    /// <summary>
    /// Roles for which this trigger does not apply.
    /// </summary>
    [DataField("jobsExept")]
    public List<String> JobsExept { get; private set; } = new List<String>();

    /// <summary>
    /// The race for which this trigger will be effective.
    /// </summary>
    [DataField("species")]
    public List<String> Species { get; private set; } = new List<String>(); // and here
}
