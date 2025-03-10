using Content.Server.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.EffectConditions;

/// <summary>
///     Requires that the user's bloodstream reagent is not the same as metabolizing
/// </summary>
public sealed partial class IsNotBloodstreamReagent : EntityEffectCondition
{
    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream) || reagentArgs.Reagent == null)
                return true;
            if (bloodstream.BloodReagent == reagentArgs.Reagent.ID)
                return false;

            return true;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-not-bloodstream-reagent");
    }
}
