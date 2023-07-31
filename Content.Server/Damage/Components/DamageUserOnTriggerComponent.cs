using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class DamageUserOnTriggerComponent : Component
{
    [DataField("resistancePenetration")]
    public float? ResistancePenetration;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;
}
