using JetBrains.Annotations;
using Content.Shared.Atmos;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Consumes and emits configured gases around plants each growth tick, then merges
/// the adjusted gas mixture back into the environment.
/// </summary>
public abstract class SharedConsumeExudeGasGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsumeExudeGasGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
    }

    private void OnCrossPollinate(Entity<ConsumeExudeGasGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<ConsumeExudeGasGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossGasses(ref ent.Comp.ConsumeGasses, pollenData.ConsumeGasses);
        _mutation.CrossGasses(ref ent.Comp.ExudeGasses, pollenData.ExudeGasses);
    }

    /// <summary>
    /// Adds a random amount of a random gas to the exude gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomExudeGasses(Entity<ConsumeExudeGasGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var gasses = ent.Comp.ExudeGasses;
        var gas = rand.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;

        DirtyField(ent, nameof(ent.Comp.ExudeGasses));
    }

    /// <summary>
    /// Adds a random amount of a random gas to the consume gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomConsumeGasses(Entity<ConsumeExudeGasGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var gasses = ent.Comp.ConsumeGasses;
        var gas = rand.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;

        DirtyField(ent, nameof(ent.Comp.ConsumeGasses));
    }
}
