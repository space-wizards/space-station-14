using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that reverts aging of plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantCryoxadoneEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantCryoxadone>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (!TryComp<PlantHolderComponent>(entity, out var plantHolder))
            return;

        var deviation = plantHolder.Age > entity.Comp.Maturation
            ? (int)Math.Max(entity.Comp.Maturation - 1, plantHolder.Age - _random.Next(7, 10))
            : (int)(entity.Comp.Maturation / entity.Comp.GrowthStages);

        _plantHarvest.AffectGrowth(entity.Owner, -deviation);
        _plant.ForceUpdateByExternalCause(entity.AsNullable());
    }
}
