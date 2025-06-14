using Content.Shared.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
///     Condition for if the entity is or isn't wearing internals.
/// </summary>
public sealed partial class Internals : EntityEffectCondition
{
    /// <summary>
    ///     To pass, the entity's internals must have this same state.
    /// </summary>
    [DataField]
    public bool UsingInternals = true;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out InternalsComponent? internalsComp))
            return !UsingInternals; // They have no internals to wear.

        var internalsState = internalsComp.GasTankEntity == null;
        return UsingInternals == internalsState;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-internals", ("usingInternals", UsingInternals));
    }
}
