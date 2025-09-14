using Content.Shared.EntityEffects;
using Content.Shared.Temperature.Components;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TemperatureEntityConditionSystem : EntityConditionSystem<TemperatureComponent, Temperature>
{
    protected override void Condition(Entity<TemperatureComponent> entity, ref EntityConditionEvent<Temperature> args)
    {
        if (entity.Comp.CurrentTemperature >= args.Condition.Min && entity.Comp.CurrentTemperature <= args.Condition.Max)
            args.Pass = true;
    }
}

public sealed class Temperature : EntityConditionBase<Temperature>
{
    /// <summary>
    /// Minimum allowed temperature
    /// </summary>
    [DataField]
    public float Min = 0;

    /// <summary>
    /// Maximum allowed temperature
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    /*
    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-body-temperature",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
    }*/
}
