using Content.Server.Atmos.EntitySystems;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Shared.Atmos;

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
        var (plantUid, component) = ent;

        if (!TryComp<PlantComponent>(plantUid, out var plant)
            || !TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        var environment = _atmosphere.GetContainingMixture(plantUid, true, true) ?? GasMixture.SpaceGas;

        // Consume Gasses.
        holder.MissingGas = 0;
        if (component.ConsumeGasses.Count > 0)
        {
            foreach (var (gas, amount) in component.ConsumeGasses)
            {
                if (environment.GetMoles(gas) < amount)
                {
                    holder.MissingGas++;
                    continue;
                }

                environment.AdjustMoles(gas, -amount);
            }

            if (holder.MissingGas > 0)
                _plantHolder.AdjustsHealth(plantUid, -holder.MissingGas);
        }

        // Exude Gasses.
        var exudeCount = component.ExudeGasses.Count;
        if (exudeCount > 0)
        {
            foreach (var (gas, amount) in component.ExudeGasses)
            {
                environment.AdjustMoles(gas,
                    MathF.Max(1f, MathF.Round(amount * MathF.Round(plant.Potency) / exudeCount)));
            }
        }
    }
}
