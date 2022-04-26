using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

// placeholder for later when projectile spells can be implemented
public sealed class ProjectileSpellEvent : WorldTargetActionEvent
{
    [DataField("projectile", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Projectile = default!;

    // does nothing for hitscan
    [DataField(("speed"))]
    public float Speed = 7f;

    // does nothing for hitscan
    [DataField("spread")]
    public float Spread = 20f;

    // does nothing for hitscan
    [DataField("quantity")]
    public int Quantity = 1;
}
