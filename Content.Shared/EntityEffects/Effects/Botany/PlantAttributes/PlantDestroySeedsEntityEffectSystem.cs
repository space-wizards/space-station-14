using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adds the <see cref="PlantTraitSeedlessComponent"/> to a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantDestroySeedsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantDestroySeeds>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantDestroySeeds> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _popup.PopupEntity(
            Loc.GetString("botany-plant-seedsdestroyed"),
            entity,
            PopupType.SmallCaution
        );
        EnsureComp<PlantTraitSeedlessComponent>(entity.Owner);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantDestroySeeds : EntityEffectBase<PlantDestroySeeds>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-seeds-remove", ("chance", Probability));
}
