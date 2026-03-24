using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Fills a ballistic magazine at MapInit with a weighted-random mix of ammo types
/// in shuffled order. Used for second-hand worn magazines.
/// </summary>
public sealed class MixedAmmoFillSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MixedAmmoFillComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(SharedGunSystem) });
    }

    private void OnMapInit(Entity<MixedAmmoFillComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(ent.Owner, out var ballistic))
            return;

        if (ent.Comp.Entries.Count == 0)
            return;

        // Compute total rounds to load
        var min = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MinFillFraction), 0, ballistic.Capacity);
        var max = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MaxFillFraction), 0, ballistic.Capacity);
        if (min > max)
            min = max;
        var count = _random.Next(min, max + 1);

        // Build weighted total
        var totalWeight = 0f;
        foreach (var entry in ent.Comp.Entries)
            totalWeight += entry.Weight;

        // Spawn each round using weighted random selection
        var coords = Transform(ent.Owner).Coordinates;
        for (var i = 0; i < count; i++)
        {
            var proto = PickWeighted(ent.Comp.Entries, totalWeight);
            var spawned = Spawn(proto, coords);
            ballistic.Entities.Add(spawned);
            _containers.Insert(spawned, ballistic.Container);
        }

        // Shuffle for random firing order (Entities list is popped from the end)
        _random.Shuffle(ballistic.Entities);

        // Clear unspawned count — all rounds are now explicit entities
        ballistic.UnspawnedCount = 0;

        Dirty(ent.Owner, ballistic);
    }

    private EntProtoId PickWeighted(List<MixedAmmoEntry> entries, float totalWeight)
    {
        var roll = _random.NextFloat() * totalWeight;
        var cumulative = 0f;
        foreach (var entry in entries)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry.Proto;
        }
        return entries[^1].Proto;
    }
}
