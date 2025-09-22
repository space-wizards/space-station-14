using Content.Shared.Chemistry.Components;

namespace Content.Shared.EntityEffects.Effects.Solution;

public sealed class SetSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, SetSolutionTemperature>
{
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<SetSolutionTemperature> args)
    {
        entity.Comp.Solution.Temperature = args.Effect.Temperature;
    }
}

public sealed class SetSolutionTemperature : EntityEffectBase<SetSolutionTemperature>
{
    /// <summary>
    ///     The temperature to set the solution to.
    /// </summary>
    [DataField(required: true)]
    public float Temperature;
}

public sealed class AdjustSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionTemperature>
{
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustSolutionTemperature> args)
    {
        var solution = entity.Comp.Solution;

        var deltaT = args.Scale * args.Effect.Delta;
        solution.Temperature = Math.Clamp(solution.Temperature + deltaT, args.Effect.MinTemp, args.Effect.MaxTemp);
    }
}

[DataDefinition]
public sealed partial class AdjustSolutionTemperature : EntityEffectBase<AdjustSolutionTemperature>
{
    /// <summary>
    ///     The change in temperature.
    /// </summary>
    [DataField(required: true)]
    public float Delta;

    /// <summary>
    ///     The minimum temperature this effect can reach.
    /// </summary>
    [DataField]
    public float MinTemp;

    /// <summary>
    ///     The maximum temperature this effect can reach.
    /// </summary>
    [DataField]
    public float MaxTemp = float.PositiveInfinity;

    /// <summary>
    ///     If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
    /// </summary>
    [DataField]
    public bool Scaled;
}

public sealed class AdjustSolutionThermalEnergyEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionThermalEnergy>
{
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustSolutionThermalEnergy> args)
    {
        var solution = entity.Comp.Solution;

        var delta = args.Scale * args.Effect.Delta;

        switch (delta)
        {
            case > 0:
                if (solution.Temperature >= args.Effect.MaxTemp)
                    return;
                break;
            case < 0:
                if (solution.Temperature <= args.Effect.MinTemp)
                    return;
                break;
            default:
                return;
        }

        var heatCap = solution.GetHeatCapacity(null);
        var deltaT = delta / heatCap;

        solution.Temperature = Math.Clamp(solution.Temperature + deltaT, args.Effect.MinTemp, args.Effect.MaxTemp);
    }
}

[DataDefinition]
public sealed partial class AdjustSolutionThermalEnergy : EntityEffectBase<AdjustSolutionThermalEnergy>
{
    /// <summary>
    ///     The change in temperature.
    /// </summary>
    [DataField(required: true)]
    public float Delta;

    /// <summary>
    ///     The minimum temperature this effect can reach.
    /// </summary>
    [DataField]
    public float MinTemp;

    /// <summary>
    ///     The maximum temperature this effect can reach.
    /// </summary>
    [DataField]
    public float MaxTemp = float.PositiveInfinity;

    /// <summary>
    ///     If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
    /// </summary>
    [DataField]
    public bool Scaled;
}
