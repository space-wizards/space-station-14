namespace Content.Shared.Damage.Components;

/// <summary>
/// Deals damage to the user (the entity that triggered the entity), such as a player stepping on a mousetrap.
/// </summary>
/// <remarks>
/// This component should be attached to the triggering object (e.g., a mousetrap).
/// </remarks>
[RegisterComponent]
public sealed partial class DamageUserOnTriggerComponent : Component
{
    [DataField] public bool IgnoreResistances;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
