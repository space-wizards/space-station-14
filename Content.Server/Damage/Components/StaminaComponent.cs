using Content.Server.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Server.Damage.Components;

/// <summary>
/// Add to an entity to paralyze it whenever it reaches critical amounts of Stamina DamageType.
/// </summary>
[RegisterComponent]
public sealed class StaminaComponent : Component
{
    /// <summary>
    /// Have we reached peak stamina damage and been paralyzed?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("critical")]
    public bool Critical;

    /// <summary>
    /// How much stamina reduces per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("decay")]
    public float Decay = 3f;

    /// <summary>
    /// How much time after receiving damage until stamina starts decreasing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public float DecayCooldown = 5f;

    /// <summary>
    /// How much stamina damage this entity has taken.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("staminaDamage")]
    public float StaminaDamage;

    /// <summary>
    /// How much stamina damage is required to entire stam crit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("excess")]
    public float CritThreshold = 100f;

    /// <summary>
    /// Next time we're allowed to decrease stamina damage. Refreshes whenever the stam damage is changed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("decayAccumulator")]
    public float StaminaDecayAccumulator;
}
