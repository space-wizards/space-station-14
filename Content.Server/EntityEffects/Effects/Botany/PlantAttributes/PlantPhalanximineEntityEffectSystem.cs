using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that mutates plant to lose health with time.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantPhalanximineEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantPhalanximine>
{
    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantPhalanximine> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        var plantUid = entity.Comp.PlantEntity.Value;
        if (TryComp<PlantTraitsComponent>(plantUid, out var traits))
            traits.Viable = true;
    }
}
