using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// This effect directly increases the potency of a plant.
/// Potency directly correlates to the size of the plant's produce.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class RobustHarvestEntityEffectSystem : EntityEffectSystem<PlantComponent, RobustHarvest>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<RobustHarvest> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (entity.Comp.Potency < args.Effect.PotencyLimit)
        {
            // Calculates and rewrites the potency value.
            var potency = Math.Min(entity.Comp.Potency + args.Effect.PotencyIncrease, args.Effect.PotencyLimit);
            _plant.AdjustPotency(entity.AsNullable(), potency - entity.Comp.Potency);

            if (entity.Comp.Potency > args.Effect.PotencySeedlessThreshold)
                EnsureComp<PlantTraitSeedlessComponent>(entity.Owner);
        }
        else if (entity.Comp.Yield > 1 && SharedRandomExtensions.PredictedProb(_timing, 0.1f, GetNetEntity(entity)))
        {
            // Too much of a good thing reduces yield.
            _plant.AdjustYield(entity.AsNullable(), -1);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class RobustHarvest : EntityEffectBase<RobustHarvest>
{
    /// <summary>
    /// The maximum potency of the plant to allow the effect to trigger.
    /// </summary>
    [DataField]
    public int PotencyLimit = 50;

    /// <summary>
    /// The amount of potency to increase the plant by.
    /// </summary>
    [DataField]
    public int PotencyIncrease = 3;

    /// <summary>
    /// The threshold at which the plant will become seedless.
    /// </summary>
    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-robust-harvest",
            ("seedlesstreshold", PotencySeedlessThreshold),
            ("limit", PotencyLimit),
            ("increase", PotencyIncrease),
            ("chance", Probability));
    }
}
