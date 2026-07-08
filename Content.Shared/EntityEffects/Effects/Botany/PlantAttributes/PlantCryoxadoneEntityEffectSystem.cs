using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

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
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantCryoxadone> args)
    {
        // No predict random.
        if (_net.IsClient)
            return;

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

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantCryoxadone : EntityEffectBase<PlantCryoxadone>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
