using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class DamageUserOnTriggerComponent : Component
{
    [DataField("reduceResistanceValue")]
    public float? ReduceResistanceValue;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;
}
