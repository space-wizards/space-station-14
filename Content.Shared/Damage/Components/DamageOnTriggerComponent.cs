namespace Content.Shared.Damage.Components;

/// <summary>
/// Deals damage to the triggered entity itself.
/// </summary>
/// <remarks>
/// The triggered entity must have a <see cref="DamageableComponent"/> (e.g., be a creature or a player) to receive damage.
/// </remarks>
[RegisterComponent]
public sealed partial class DamageOnTriggerComponent : Component
{
    [DataField] public bool IgnoreResistances;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
