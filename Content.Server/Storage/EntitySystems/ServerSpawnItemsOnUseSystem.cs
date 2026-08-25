using Content.Server.Cargo.Systems;
using Content.Shared.Cargo;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Map;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.Storage.EntitySystems;

/// <inheritdoc/>
public sealed partial class ServerSpawnItemsOnUseSystem : SpawnItemsOnUseSystem
{
    [Dependency] private PricingSystem _pricing = default!;

    [SubscribeLocalEvent]
    private void CalculatePrice(EntityUid uid, SpawnItemsOnUseComponent component, ref PriceCalculationEvent args)
    {
        var ungrouped = CollectOrGroups(component.Items, out var orGroups);

        foreach (var entry in ungrouped)
        {
            var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

            // Calculate the average price of the possible spawned items
            args.Price += _pricing.GetPrice(protUid) * entry.SpawnProbability * entry.GetAmount(getAverage: true);

            Del(protUid);
        }

        foreach (var group in orGroups)
        {
            foreach (var entry in group.Entries)
            {
                var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                // Calculate the average price of the possible spawned items
                args.Price += _pricing.GetPrice(protUid) *
                              (entry.SpawnProbability / group.CumulativeProbability) *
                              entry.GetAmount(getAverage: true);

                Del(protUid);
            }
        }

        args.Handled = true;
    }
}
