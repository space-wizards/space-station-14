using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.EffectConditions;

/// <summary>
///     Requires the solution to be above or below a certain temperature.
///     Used for things like explosives.
/// </summary>
public sealed partial class SolutionTemperature : EntityEffectCondition
{
    [DataField]
    public float Min = 0.0f;

    [DataField]
    public float Max = float.PositiveInfinity;
    public override bool Condition(EntityEffectArgs args)
    {
        if (args.Source == null)
            return false;
        if (args.Source.Temperature < Min)
            return false;
        if (args.Source.Temperature > Max)
            return false;

        return true;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-solution-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
    }
}
