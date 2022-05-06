using Content.Shared.Damage;

namespace Content.Server.Medical.Components;

/// <summary>
/// For healing triggers.
/// </summary>
[RegisterComponent]
public sealed class HealOnTriggerComponent : Component
{
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;
}
