using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Popups;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Content.Shared.Popups;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that removes ability to get seeds from plant using seed maker.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantDestroySeedsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantDestroySeeds>
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantDestroySeeds> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        if (!TryComp<PlantTraitsComponent>(entity, out var traits)
            || traits.Seedless)
            return;

        _popup.PopupEntity(
            Loc.GetString("botany-plant-seedsdestroyed"),
            entity,
            PopupType.SmallCaution
        );
        traits.Seedless = true;
    }
}
