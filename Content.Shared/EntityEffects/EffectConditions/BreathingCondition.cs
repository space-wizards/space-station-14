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

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out RespiratorComponent? respiratorComp))
            return !IsBreathing; // They do not breathe.

        var breathingState = args.EntityManager.System<RespiratorSystem>().IsBreathing((args.TargetEntity, respiratorComp));
        return IsBreathing == breathingState;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-breathing",
                            ("isBreathing", IsBreathing));
    }
}
