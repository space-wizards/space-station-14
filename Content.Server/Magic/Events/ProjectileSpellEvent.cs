using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public sealed class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")] public MagicSpawnData Pos = new TargetCasterPos();
}
