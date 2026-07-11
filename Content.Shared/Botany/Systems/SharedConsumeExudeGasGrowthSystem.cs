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
public abstract partial class SharedPlantConsumeExudeGasSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MutationSystem _mutation = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantConsumeExudeGasComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantConsumeExudeGasComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossGasses(ent, ref ent.Comp.ConsumeGasses, pollenData.ConsumeGasses);
        _mutation.CrossGasses(ent, ref ent.Comp.ExudeGasses, pollenData.ExudeGasses);
        Dirty(ent);
    }

    /// <summary>
    /// Adds a random amount of a random gas to the exude gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomExudeGasses(Entity<PlantConsumeExudeGasComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var gas = random.Pick(Enum.GetValues<Gas>());

        var gasses = ent.Comp.ExudeGasses;
        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;

        DirtyField(ent, nameof(ent.Comp.ExudeGasses));
    }

    /// <summary>
    /// Adds a random amount of a random gas to the consume gasses.
    /// </summary>
    [PublicAPI]
    public void MutateRandomConsumeGasses(Entity<PlantConsumeExudeGasComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var gasses = ent.Comp.ConsumeGasses;
        var gas = random.Pick(Enum.GetValues<Gas>());

        if (!gasses.TryAdd(gas, amount))
            gasses[gas] += amount;

        DirtyField(ent, nameof(ent.Comp.ConsumeGasses));
    }
}
