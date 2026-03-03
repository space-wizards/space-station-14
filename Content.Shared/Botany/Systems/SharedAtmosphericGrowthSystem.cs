using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Applies atmospheric temperature and pressure effects to plants during growth ticks.
/// Uses current tile gas mixture to penalize or clear warnings based on tolerances.
/// </summary>
public abstract class SharedAtmosphericGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AtmosphericGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
    }

    private void OnCrossPollinate(Entity<AtmosphericGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<AtmosphericGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.LowHeatTolerance, pollenData.LowHeatTolerance);
        _mutation.CrossFloat(ref ent.Comp.HighHeatTolerance, pollenData.HighHeatTolerance);
        _mutation.CrossFloat(ref ent.Comp.LowPressureTolerance, pollenData.LowPressureTolerance);
        _mutation.CrossFloat(ref ent.Comp.HighPressureTolerance, pollenData.HighPressureTolerance);
    }


    /// <summary>
    /// Adjusts minimum temperature tolerance for plant growth.
    /// Ensures low temperature is not greater than high.
    /// </summary>
    public void AdjustLowHeatTolerance(Entity<AtmosphericGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.LowHeatTolerance = MathF.Max(0f, ent.Comp.LowHeatTolerance + amount);
        if (ent.Comp.LowHeatTolerance > ent.Comp.HighHeatTolerance)
            ent.Comp.HighHeatTolerance = ent.Comp.LowHeatTolerance;

        Dirty(ent);
    }

    /// <summary>
    /// Adjusts maximum temperature tolerance for plant growth.
    /// Ensures low temperature is not less than high.
    /// </summary>
    public void AdjustHighHeatTolerance(Entity<AtmosphericGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.HighHeatTolerance = MathF.Max(0f, ent.Comp.HighHeatTolerance + amount);
        if (ent.Comp.HighHeatTolerance < ent.Comp.LowHeatTolerance)
            ent.Comp.LowHeatTolerance = ent.Comp.HighHeatTolerance;

        Dirty(ent);
    }

    /// <summary>
    /// Adjusts minimum pressure tolerance for plant growth.
    /// Ensures pressure low is not greater than high.
    /// </summary>
    [PublicAPI]
    public void AdjustLowPressureTolerance(Entity<AtmosphericGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.LowPressureTolerance = MathF.Max(0f, ent.Comp.LowPressureTolerance + amount);
        if (ent.Comp.LowPressureTolerance > ent.Comp.HighPressureTolerance)
            ent.Comp.HighPressureTolerance = ent.Comp.LowPressureTolerance;

        Dirty(ent);
    }

    /// <summary>
    /// Adjusts maximum pressure tolerance for plant growth.
    /// Ensures pressure high is not less than low.
    /// </summary>
    [PublicAPI]
    public void AdjustHighPressureTolerance(Entity<AtmosphericGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.HighPressureTolerance = MathF.Max(0f, ent.Comp.HighPressureTolerance + amount);
        if (ent.Comp.HighPressureTolerance < ent.Comp.LowPressureTolerance)
            ent.Comp.LowPressureTolerance = ent.Comp.HighPressureTolerance;

        Dirty(ent);
    }
}
