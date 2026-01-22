using System.Linq;
using Content.Server.Botany.Components;
using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany;

public sealed partial class PlantMutateExudeGasesEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateExudeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateExudeGases> args)
    {
        if (entity.Comp.Seed == null)
            return;

        var gasses = entity.Comp.Seed.ExudeGasses;

        // Add a random amount of a random gas to this gas dictionary
        float amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        var gas = _random.Pick(Enum.GetValues(typeof(Gas)).Cast<Gas>().ToList());

        if (!gasses.TryAdd(gas, amount))
        {
            gasses[gas] += amount;
        }
    }
}

public sealed partial class PlantMutateConsumeGasesEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateConsumeGases>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateConsumeGases> args)
    {
        if (entity.Comp.Seed == null)
            return;

        var gasses = entity.Comp.Seed.ConsumeGasses;

        // Add a random amount of a random gas to this gas dictionary
        var amount = _random.NextFloat(args.Effect.MinValue, args.Effect.MaxValue);
        var gas = _random.Pick(Enum.GetValues(typeof(Gas)).Cast<Gas>().ToList());

        if (!gasses.TryAdd(gas, amount))
        {
            gasses[gas] += amount;
        }
    }
}

