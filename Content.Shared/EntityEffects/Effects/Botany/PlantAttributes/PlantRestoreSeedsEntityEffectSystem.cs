using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that restores ability to get seeds from plant seed maker.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantRestoreSeedsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantRestoreSeeds>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantRestoreSeeds> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _popup.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), entity);
        _plantTraits.DelTrait(entity.Owner, new TraitSeedless());
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantRestoreSeeds : EntityEffectBase<PlantRestoreSeeds>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-seeds-add", ("chance", Probability));
}
