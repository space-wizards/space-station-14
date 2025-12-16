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
public sealed partial class PlantDestroySeedsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantDestroySeeds>
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantDestroySeeds> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;

        var plantUid = entity.Comp.PlantEntity!.Value;
        if (!TryComp<PlantHolderComponent>(plantUid, out var plantHolder) || plantHolder.Dead)
            return;

        if (!TryComp<PlantTraitsComponent>(plantUid, out var traits) || traits.Seedless)
            return;

        _popup.PopupEntity(
            Loc.GetString("botany-plant-seedsdestroyed"),
            entity,
            PopupType.SmallCaution
        );
        traits.Seedless = true;
    }
}
