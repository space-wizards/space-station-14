using Content.Server.Botany.Components;
using Content.Server.Botany.Events;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles toxin accumulation and tolerance for plants, applying health damage
/// and decrementing toxins based on per-tick uptake.
/// </summary>
public sealed class ToxinsSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

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
        var (plantUid, component) = ent;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        if (holder.Toxins < 0)
            return;

        var toxinUptake = MathF.Max(1, MathF.Round(holder.Toxins / component.ToxinUptakeDivisor));
        if (holder.Toxins > component.ToxinsTolerance)
            holder.Health -= toxinUptake;

        // there is a possibility that it will remove more toxin than amount of damage it took on plant health (and killed it).
        // TODO: get min out of health left and toxin uptake - would work better, probably.
        holder.Toxins -= toxinUptake;
    }
}
