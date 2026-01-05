using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that enhances plant longevity and endurance.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantDiethylamineEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantDiethylamine>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantDiethylamine> args)
    {
        // No predict random.
        if (_net.IsClient)
            return;

        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (_random.Prob(0.1f))
            _plant.AdjustLifespan(entity.AsNullable(), 1);

        if (_random.Prob(0.1f))
            _plant.AdjustEndurance(entity.AsNullable(), 1);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantDiethylamine : EntityEffectBase<PlantDiethylamine>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-diethylamine", ("chance", Probability));
}
