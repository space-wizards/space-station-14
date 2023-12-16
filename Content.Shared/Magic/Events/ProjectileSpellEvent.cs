using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Magic.Events;

// TODO: Might need to be combined into InstantSpawnSpellEvent
public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent, ISpeakSpell
{
    // TODO: Move to magic component
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    // TODO: Check if still needed, then move to magic component if possible
    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")] public MagicSpawnData Pos = new TargetInFront();

    // TODO: Move to magic component
    [DataField("speech")]
    public string? Speech { get; private set; }
}
