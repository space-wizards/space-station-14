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

    // TODO: Move to magic component
    [DataField("speech")]
    public string? Speech { get; private set; }
}
