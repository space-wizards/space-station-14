using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Applies atmospheric temperature and pressure effects to plants during growth ticks.
/// Uses current tile gas mixture to penalize or clear warnings based on tolerances.
/// </summary>
public abstract partial class SharedPlantAtmosphericSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantAtmosphericComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantAtmosphericComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ent, ref ent.Comp.LowHeatTolerance, pollenData.LowHeatTolerance);
        _mutation.CrossFloat(ent, ref ent.Comp.HighHeatTolerance, pollenData.HighHeatTolerance);
        _mutation.CrossFloat(ent, ref ent.Comp.LowPressureTolerance, pollenData.LowPressureTolerance);
        _mutation.CrossFloat(ent, ref ent.Comp.HighPressureTolerance, pollenData.HighPressureTolerance);
        Dirty(ent);
    }

    /// <summary>
    /// Adjusts minimum temperature tolerance for plant growth.
    /// Ensures low temperature is not greater than high.
    /// </summary>
    public void AdjustLowHeatTolerance(Entity<PlantAtmosphericComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.LowHeatTolerance = MathF.Max(0f, ent.Comp.LowHeatTolerance + amount);
        if (ent.Comp.LowHeatTolerance > ent.Comp.HighHeatTolerance)
            ent.Comp.HighHeatTolerance = ent.Comp.LowHeatTolerance;

        DirtyFields(ent, null, nameof(ent.Comp.LowHeatTolerance), nameof(ent.Comp.HighHeatTolerance));
    }

    /// <summary>
    /// Adjusts maximum temperature tolerance for plant growth.
    /// Ensures low temperature is not less than high.
    /// </summary>
    public void AdjustHighHeatTolerance(Entity<PlantAtmosphericComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.HighHeatTolerance = MathF.Max(0f, ent.Comp.HighHeatTolerance + amount);
        if (ent.Comp.HighHeatTolerance < ent.Comp.LowHeatTolerance)
            ent.Comp.LowHeatTolerance = ent.Comp.HighHeatTolerance;

        DirtyFields(ent, null, nameof(ent.Comp.HighHeatTolerance), nameof(ent.Comp.LowHeatTolerance));
    }

    /// <summary>
    /// Adjusts minimum pressure tolerance for plant growth.
    /// Ensures pressure low is not greater than high.
    /// </summary>
    [PublicAPI]
    public void AdjustLowPressureTolerance(Entity<PlantAtmosphericComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.LowPressureTolerance = MathF.Max(0f, ent.Comp.LowPressureTolerance + amount);
        if (ent.Comp.LowPressureTolerance > ent.Comp.HighPressureTolerance)
            ent.Comp.HighPressureTolerance = ent.Comp.LowPressureTolerance;

        DirtyFields(ent, null, nameof(ent.Comp.LowPressureTolerance), nameof(ent.Comp.HighPressureTolerance));
    }

    /// <summary>
    /// Adjusts maximum pressure tolerance for plant growth.
    /// Ensures pressure high is not less than low.
    /// </summary>
    [PublicAPI]
    public void AdjustHighPressureTolerance(Entity<PlantAtmosphericComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.HighPressureTolerance = MathF.Max(0f, ent.Comp.HighPressureTolerance + amount);
        if (ent.Comp.HighPressureTolerance < ent.Comp.LowPressureTolerance)
            ent.Comp.LowPressureTolerance = ent.Comp.HighPressureTolerance;

        DirtyFields(ent, null, nameof(ent.Comp.HighPressureTolerance), nameof(ent.Comp.LowPressureTolerance));
    }
}
