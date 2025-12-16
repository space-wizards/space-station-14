using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that enhances plant longevity and endurance.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantDiethylamineEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantDiethylamine>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantDiethylamine> args)
    {
        if (!_plantTray.HasPlant(entity.AsNullable()))
            return;


        var plantUid = entity.Comp.PlantEntity!.Value;
        if (!TryComp<PlantComponent>(plantUid, out var plant))
            return;

        if (_random.Prob(0.1f))
            plant.Lifespan++;

        if (_random.Prob(0.1f))
            plant.Endurance++;
    }
}
