using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

// TODO: Energy conservation!!! Once HeatContainers are merged nuke this and everything in SolutionContainerSystem to respect energy conservation!
/// <summary>
/// Sets the temperature of this solution to a fixed value.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class SetSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, SetSolutionTemperature>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<SetSolutionTemperature> args)
    {
        _solutionContainer.SetTemperature(entity, args.Effect.Temperature);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SetSolutionTemperature : EntityEffectBase<SetSolutionTemperature>
{
    /// <summary>
    ///     The temperature to set the solution to.
    /// </summary>
    [DataField(required: true)]
    public float Temperature;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-set-solution-temperature-effect",
            ("chance", Probability),
            ("temperature", Temperature));
}

/// <summary>
/// Adjusts the temperature of this solution by a given amount.
/// The temperature adjustment is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class AdjustSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionTemperature>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustSolutionTemperature> args)
    {
        var solution = entity.Comp.Solution;
        var temperature = Math.Clamp(solution.Temperature + args.Scale * args.Effect.Delta, args.Effect.MinTemp, args.Effect.MaxTemp);

        _solutionContainer.SetTemperature(entity, temperature);
    }
}

/// <inheritdoc cref="EntityEffect"/>
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

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-adjust-solution-temperature-effect",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Delta)),
            ("mintemp", MinTemp),
            ("maxtemp", MaxTemp));
}

/// <summary>
/// Adjusts the thermal energy of this solution by a given amount.
/// The energy adjustment is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class AdjustSolutionThermalEnergyEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionThermalEnergy>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AdjustSolutionThermalEnergy> args)
    {
        var solution = entity.Comp.Solution;

        var delta = args.Scale * args.Effect.Delta;

        // Don't adjust thermal energy if we're already at or above max temperature.
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

        _solutionContainer.AddThermalEnergyClamped(entity, delta, args.Effect.MinTemp, args.Effect.MaxTemp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustSolutionThermalEnergy : EntityEffectBase<AdjustSolutionThermalEnergy>
{
    /// <summary>
    ///     The change in thermal energy.
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

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-adjust-solution-temperature-effect",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Delta)),
            ("mintemp", MinTemp),
            ("maxtemp", MaxTemp));
}
