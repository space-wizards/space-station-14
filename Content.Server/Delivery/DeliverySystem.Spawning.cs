using Content.Server.Power.EntitySystems;
using Content.Server.StationRecords;
using Content.Shared.Delivery;
using Content.Shared.EntityTable;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Delivery;

/// <summary>
/// System for managing deliveries spawned by the mail teleporter.
/// This covers for spawning deliveries.
/// </summary>
public sealed partial class DeliverySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    private void InitializeSpawning()
    {
        SubscribeLocalEvent<CargoDeliveryDataComponent, MapInitEvent>(OnDataMapInit);
    }

    private void OnDataMapInit(Entity<CargoDeliveryDataComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDelivery = _timing.CurTime + ent.Comp.MinDeliveryCooldown; // We want an early wave of mail so cargo doesn't have to wait
    }

    private void SpawnDelivery(Entity<DeliverySpawnerComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var coords = Transform(ent).Coordinates;

        _audio.PlayPvs(ent.Comp.SpawnSound, ent.Owner);

        for (int i = 0; i < amount; i++)
        {
            var spawns = _entityTable.GetSpawns(ent.Comp.Table);

            foreach (var id in spawns)
            {
                Spawn(id, coords);
            }
        }
    }

    private void SpawnStationDeliveries(Entity<CargoDeliveryDataComponent> ent)
    {
        if (!TryComp<StationRecordsComponent>(ent, out var records))
            return;

        var spawners = GetValidSpawners(ent);

        // Skip if theres no spawners available
        if (spawners.Count == 0)
            return;

        // Skip if there's nobody in crew manifest
        if (records.Records.Keys.Count == 0)
            return;

        // We take the amount of mail calculated based on player amount or the minimum, whichever is higher.
        // We don't want stations with less than the player ratio to not get mail at all
        var initialDeliveryCount = (int)Math.Ceiling(records.Records.Keys.Count / ent.Comp.PlayerToDeliveryRatio);
        var deliveryCount = Math.Max(initialDeliveryCount, ent.Comp.MinimumDeliverySpawn);

        if (!ent.Comp.DistributeRandomly)
        {
            foreach (var spawner in spawners)
            {
                SpawnDelivery(spawner, deliveryCount);
            }
        }
        else
        {
            int[] amounts = new int[spawners.Count];

            // Distribute items randomly
            for (int i = 0; i < deliveryCount; i++)
            {
                var randomListIndex = _random.Next(spawners.Count);
                amounts[randomListIndex]++;
            }
            for (int j = 0; j < spawners.Count; j++)
            {
                SpawnDelivery(spawners[j], amounts[j]);
            }
        }

    }

    private List<EntityUid> GetValidSpawners(Entity<CargoDeliveryDataComponent> ent)
    {
        var validSpawners = new List<EntityUid>();

        var spawners = EntityQueryEnumerator<DeliverySpawnerComponent>();
        while (spawners.MoveNext(out var spawnerUid, out _))
        {
            var spawnerStation = _station.GetOwningStation(spawnerUid);

            if (spawnerStation != ent.Owner)
                continue;

            if (!_power.IsPowered(spawnerUid))
                continue;

            validSpawners.Add(spawnerUid);
        }

        return validSpawners;
    }

    private void UpdateSpawner(float frameTime)
    {
        var dataQuery = EntityQueryEnumerator<CargoDeliveryDataComponent>();
        var curTime = _timing.CurTime;

        while (dataQuery.MoveNext(out var uid, out var deliveryData))
        {
            if (deliveryData.NextDelivery > curTime)
                continue;

            deliveryData.NextDelivery += _random.Next(deliveryData.MinDeliveryCooldown, deliveryData.MaxDeliveryCooldown); // Random cooldown between min and max
            SpawnStationDeliveries((uid, deliveryData));
        }
    }
}
