using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that mutates plant to lose health with time.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantPhalanximineEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantPhalanximine>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTrait = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantPhalanximine> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantTrait.DelTrait(entity.Owner, new TraitUnviable());
    }
}
