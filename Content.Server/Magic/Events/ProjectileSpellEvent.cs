using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

/// <summary>
///     Spell that summons a projectile (entity with projectile OR hitscan component).
/// </summary>
public sealed class ProjectileSpellEvent : PerformWorldTargetActionEvent
{
    [DataField("projectile", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Projectile = default!;

    // does nothing for hitscan
    [DataField("speed")]
    public float Speed = 7f;

    // does nothing for hitscan
    [DataField("spread")]
    public float Spread = 20f;

    // does nothing for hitscan
    [DataField("quantity")]
    public int Quantity = 1;
}
