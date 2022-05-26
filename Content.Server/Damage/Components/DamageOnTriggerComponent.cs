using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class DamageOnTriggerComponent : Component
{
    [DataField("ignoreResistances")] public bool IgnoreResistances;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;
}
