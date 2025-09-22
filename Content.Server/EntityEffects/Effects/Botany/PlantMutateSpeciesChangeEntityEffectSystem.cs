using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateSpeciesChange>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Seed.MutationPrototypes.Count == 0)
            return;

        var targetProto = _random.Pick(entity.Comp.Seed.MutationPrototypes);
        _proto.TryIndex(targetProto, out SeedPrototype? protoSeed);

        if (protoSeed == null)
        {
            Log.Error($"Seed prototype could not be found: {targetProto}!");
            return;
        }

        entity.Comp.Seed = entity.Comp.Seed.SpeciesChange(protoSeed);
    }
}
