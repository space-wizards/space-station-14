using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that enhances plant longevity and endurance.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantDiethylamineEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantDiethylamine>
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantDiethylamine> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(entity));
        if (random.Prob(0.1f))
            _plant.AdjustLifespan(entity.AsNullable(), 1);

        if (random.Prob(0.1f))
            _plant.AdjustEndurance(entity.AsNullable(), 1);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantDiethylamine : EntityEffectBase<PlantDiethylamine>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-diethylamine", ("chance", Probability));
}
