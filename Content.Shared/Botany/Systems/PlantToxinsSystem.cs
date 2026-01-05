using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles toxin accumulation and tolerance for plants, applying health damage
/// and decrementing toxins based on per-tick uptake.
/// </summary>
public sealed class PlantToxinsSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantToxinsComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<PlantToxinsComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<PlantToxinsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantToxinsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.ToxinsTolerance, pollenData.ToxinsTolerance);
        _mutation.CrossFloat(ref ent.Comp.ToxinUptakeDivisor, pollenData.ToxinUptakeDivisor);
    }

    private void OnPlantGrow(Entity<PlantToxinsComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        if (ent.Comp.ToxinUptakeDivisor <= 0)
            return;

        var toxinUptake = MathF.Max(1, MathF.Round(holder.Toxins / ent.Comp.ToxinUptakeDivisor));
        if (holder.Toxins > ent.Comp.ToxinsTolerance)
            _plantHolder.AdjustsHealth(ent.Owner, -toxinUptake);

        // there is a possibility that it will remove more toxin than amount of damage it took on plant health (and killed it).
        // TODO: get min out of health left and toxin uptake - would work better, probably.
        _plantHolder.AdjustsToxins(ent.Owner, -toxinUptake);
    }

    /// <summary>
    /// Adjusts maximum toxin level the plant can tolerate before taking damage.
    /// </summary>
    [PublicAPI]
    public void AdjustToxinsTolerance(Entity<PlantToxinsComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.ToxinsTolerance = MathF.Max(0f, ent.Comp.ToxinsTolerance + amount);
        DirtyField(ent, nameof(ent.Comp.ToxinsTolerance));
    }
}
