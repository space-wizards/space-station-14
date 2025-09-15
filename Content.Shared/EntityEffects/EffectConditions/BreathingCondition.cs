using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
///     Condition for if the entity is successfully breathing.
/// </summary>
public sealed partial class Breathing : EventEntityEffectCondition<Breathing>
{
    /// <summary>
    ///     If true, the entity must not have trouble breathing to pass.
    /// </summary>
    [DataField]
    public bool IsBreathing = true;

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-breathing",
                            ("isBreathing", IsBreathing));
    }
}
