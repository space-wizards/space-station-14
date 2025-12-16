using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Popups;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that restores ability to get seeds from plant seed maker.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantRestoreSeedsEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantRestoreSeeds>
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantRestoreSeeds> args)
    {
        if (!_plantTray.HasPlantAlive(entity.AsNullable()))
            return;

        var plantUid = entity.Comp.PlantEntity!.Value;
        if (!TryComp<PlantTraitsComponent>(plantUid, out var traits) || !traits.Seedless)
            return;

        _popup.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), entity);
        traits.Seedless = false;
    }
}
