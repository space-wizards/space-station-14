using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
///     Requires the target entity to be above or below a certain temperature.
///     Used for things like cryoxadone and pyroxadone.
/// </summary>
public sealed partial class Temperature : EntityEffectCondition
{
    [DataField]
    public float Min = 0;

    [DataField]
    public float Max = float.PositiveInfinity;
    public override bool Condition(EntityEffectBaseArgs args)
    {
        var evt = new CheckEntityEffectConditionEvent<Temperature> { Condition = this, Args = args };
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
        return evt.Result;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-body-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
    }
}
