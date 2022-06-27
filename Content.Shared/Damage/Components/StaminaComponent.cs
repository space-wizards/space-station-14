using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Add to an entity to paralyze it whenever it reaches critical amounts of Stamina DamageType.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class StaminaComponent : Component
{
    /// <summary>
    /// Have we reached peak stamina damage and been paralyzed?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("critical")]
    public bool Critical;

    /// <summary>
    /// How much damage reduces per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("decay")]
    public DamageSpecifier Decay = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>()
        {
            {
                StaminaSystem.StaminaDamageType, FixedPoint2.New(-3)
            }
        }
    };

    /// <summary>
    /// How much stamina damage we're allowed to have above our critical threshold.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("excess")]
    public FixedPoint2 CritExcess = FixedPoint2.New(20f);

    /// <summary>
    /// Next time we're allowed to decrease stamina damage. Refreshes whenever the stam damage is changed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("decayAccumulator")]
    public float StaminaDecayAccumulator = 0f;
}
