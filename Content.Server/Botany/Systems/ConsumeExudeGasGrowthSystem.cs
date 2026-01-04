using JetBrains.Annotations;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Shared.Atmos;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Consumes and emits configured gases around plants each growth tick, then merges
/// the adjusted gas mixture back into the environment.
/// </summary>
public sealed class ConsumeExudeGasGrowthSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsumeExudeGasGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<ConsumeExudeGasGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<ConsumeExudeGasGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<ConsumeExudeGasGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossGasses(ref ent.Comp.ConsumeGasses, pollenData.ConsumeGasses);
        _mutation.CrossGasses(ref ent.Comp.ExudeGasses, pollenData.ExudeGasses);
    }

    private void OnPlantGrow(Entity<ConsumeExudeGasGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantComponent>(ent.Owner, out var plant)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var plantHolder))
            return;

        var environment = _atmosphere.GetContainingMixture(ent.Owner, true, true) ?? GasMixture.SpaceGas;

        // Consume Gasses.
        plantHolder.MissingGas = false;
        var missingGas = 0;
        if (ent.Comp.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in ent.Comp.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    missingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (missingGas > 0)
            {
                _plantHolder.AdjustsHealth(ent.Owner, -missingGas);
                plantHolder.MissingGas = true;
            }
        }

        // Exude Gasses.
        var exudeCount = ent.Comp.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in ent.Comp.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(amount * MathF.Round(plant.Potency) / exudeCount)));
            }
        }
    }

    /// <summary>
    /// Adds a random amount of a random gas to the exude gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomExudeGasses(Entity<ConsumeExudeGasGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var gasses = ent.Comp.ExudeGasses;
        var gas = _random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;
    }

    /// <summary>
    /// Adds a random amount of a random gas to the consume gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomConsumeGasses(Entity<ConsumeExudeGasGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var gasses = ent.Comp.ConsumeGasses;
        var gas = _random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;
    }
}
