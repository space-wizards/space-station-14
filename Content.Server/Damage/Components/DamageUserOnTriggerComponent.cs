using Content.Shared.Damage;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed partial class DamageUserOnTriggerComponent : Component
{
    [DataField] public bool IgnoreResistances;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
