using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

// TODO: Energy conservation!!! Once HeatContainers are merged nuke this and everything in SolutionContainerSystem to respect energy conservation!
/// <summary>
/// Sets the temperature of this solution to a fixed value.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SetSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, SetSolutionTemperature>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, SetSolutionTemperature effect, EntityEffectData data)
    {
        _solutionContainer.SetTemperature(entity, effect.Temperature);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SetSolutionTemperature : EntityEffect
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
public sealed partial class AdjustSolutionTemperatureEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionTemperature>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, AdjustSolutionTemperature effect, EntityEffectData data)
    {
        var solution = entity.Comp.Solution;
        var temperature = Math.Clamp(solution.Temperature + data.Scale * effect.Delta, effect.MinTemp, effect.MaxTemp);

        _solutionContainer.SetTemperature(entity, temperature);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustSolutionTemperature : EntityEffect
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
public sealed partial class AdjustSolutionThermalEnergyEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustSolutionThermalEnergy>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> entity, AdjustSolutionThermalEnergy effect, EntityEffectData data)
    {
        var solution = entity.Comp.Solution;

        var delta = data.Scale * effect.Delta;

        // Don't adjust thermal energy if we're already at or above max temperature.
        switch (delta)
        {
            case > 0:
                if (solution.Temperature >= effect.MaxTemp)
                    return;
                break;
            case < 0:
                if (solution.Temperature <= effect.MinTemp)
                    return;
                break;
            default:
                return;
        }

        _solutionContainer.AddThermalEnergyClamped(entity, delta, effect.MinTemp, effect.MaxTemp);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AdjustSolutionThermalEnergy : EntityEffect
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
