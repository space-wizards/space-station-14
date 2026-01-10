using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that removes the <see cref="PlantTraitUnviableComponent"/> from a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantPhalanximineEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantPhalanximine>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantPhalanximine> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        RemCompDeferred<PlantTraitUnviableComponent>(entity.Owner);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantPhalanximine : EntityEffectBase<PlantPhalanximine>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-phalanximine", ("chance", Probability));
}
