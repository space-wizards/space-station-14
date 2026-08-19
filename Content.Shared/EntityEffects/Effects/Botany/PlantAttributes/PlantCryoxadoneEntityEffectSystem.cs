using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that reverts aging of plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantCryoxadone>
{
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (!TryComp<PlantHolderComponent>(entity, out var plantHolder))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var deviation = plantHolder.Age > entity.Comp.Maturation
            ? (int)Math.Max(entity.Comp.Maturation - 1, plantHolder.Age - random.Next(7, 10))
            : (int)(entity.Comp.Maturation / entity.Comp.GrowthStages);

        _plantHarvest.AffectGrowth(entity.Owner, -deviation);
        _plant.ForceUpdate(entity.AsNullable());
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantCryoxadone : EntityEffectBase<PlantCryoxadone>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
