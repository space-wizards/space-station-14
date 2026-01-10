using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Robust.Shared.Random;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Manages weed growth and pest damage per growth tick, and handles tray-level
/// weed spawning.
/// </summary>
public sealed class WeedPestGrowthSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeedPestGrowthComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<WeedPestGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnCrossPollinate(Entity<WeedPestGrowthComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<WeedPestGrowthComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ref ent.Comp.WeedTolerance, pollenData.WeedTolerance);
        _mutation.CrossFloat(ref ent.Comp.PestTolerance, pollenData.PestTolerance);
    }

    private void OnPlantGrow(Entity<WeedPestGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        if (_random.Prob(ent.Comp.PestGrowthChance))
            _plantHolder.AdjustsPests(ent.Owner, ent.Comp.PestGrowthAmount);

        if (holder.PestLevel > ent.Comp.PestTolerance)
            _plantHolder.AdjustsHealth(ent.Owner, -ent.Comp.PestDamageAmount);
    }

    /// <summary>
    /// Adjusts maximum weed level the plant can tolerate before taking damage.
    /// </summary>
    [PublicAPI]
    public void AdjustWeedTolerance(Entity<WeedPestGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.WeedTolerance = MathF.Max(0f, ent.Comp.WeedTolerance + amount);
        DirtyField(ent, nameof(ent.Comp.WeedTolerance));
    }

    /// <summary>
    /// Adjusts maximum pest level the plant can tolerate before taking damage.
    /// </summary>
    [PublicAPI]
    public void AdjustPestTolerance(Entity<WeedPestGrowthComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.PestTolerance = MathF.Max(0f, ent.Comp.PestTolerance + amount);
        DirtyField(ent, nameof(ent.Comp.PestTolerance));
    }

    [PublicAPI]
    public bool GetPestThreshold(Entity<WeedPestGrowthComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return false;

        return holder.PestLevel > ent.Comp.PestTolerance;
    }
}
