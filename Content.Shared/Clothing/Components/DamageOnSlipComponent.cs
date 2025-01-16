using Content.Shared.Damage;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// A component to give clothing a chance to be damaged when slipping
/// </summary>
/// <remarks>
/// Pairs best with a `Destructible` component
/// </remarks>
[RegisterComponent]
public sealed partial class DamageOnSlipComponent : Component
{
    /// <summary>
    /// Chance the clothing will be damaged when slipped
    /// </summary>
    [DataField]
    public float DamageChance = 0.01f;

    /// <summary>
    /// Damage per instance of unlucky slip
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage;

    /// <summary>
    /// Damage multiplier maximum
    /// </summary>
    /// <remarks>
    /// Will multiply the damage specifier by a random float from 1 to maximum (non inclusive)
    /// </remarks>
    [DataField]
    public float? MultiplierMax;
}
