using Content.Server.Power.Components;
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

    private void InitializeSpawning()
    {
        SubscribeLocalEvent<CargoDeliveryDataComponent, MapInitEvent>(OnDataMapInit);
    }

    private void OnDataMapInit(Entity<CargoDeliveryDataComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDelivery = _timing.CurTime + _random.Next(ent.Comp.MinDeliveryCooldown, ent.Comp.MaxDeliveryCooldown);
    }

    private void SpawnDelivery(Entity<DeliverySpawnerComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        var coords = Transform(ent).Coordinates;

        PlaySpawnSound((ent.Owner, ent.Comp));

        while (amount > 0)
        {
            foreach (var id in spawns)
            {
                Spawn(id, coords);
            }

            amount--;
        }
    }

    private void PlaySpawnSound(Entity<DeliverySpawnerComponent> ent)
    {
        if (ent.Comp.NextSoundTime > _timing.CurTime)
            return;

        if (ent.Comp.SpawnSound != null)
        {
            _audio.PlayPvs(ent.Comp.SpawnSound, ent.Owner);
            ent.Comp.NextSoundTime = _timing.CurTime + ent.Comp.SpawnSoundCooldown;
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

        // We take the amount of mail calculated based on player amount or the minimum, whichever is higher.
        // We don't want stations with less than the player ratio to not get mail at all
        var deliveryCount = Math.Max(records.Records.Keys.Count / ent.Comp.PlayerToDeliveryRatio, ent.Comp.MinimumDeliverySpawn);

        if (!ent.Comp.DistributeRandomly)
        {
            foreach (var spawner in spawners)
            {
                SpawnDelivery(spawner, deliveryCount);
            }
        }
        else
        {
            var deliverySpawnAmount = deliveryCount;
            while (deliverySpawnAmount > 0)
            {
                var targetSpawner = _random.Pick(spawners);

                SpawnDelivery(targetSpawner, 1);

                deliverySpawnAmount--;
            }
        }

    }

    private List<EntityUid> GetValidSpawners(Entity<CargoDeliveryDataComponent> ent)
    {
        var validSpawners = new List<EntityUid>();

        var spawners = EntityQueryEnumerator<DeliverySpawnerComponent>();
        while (spawners.MoveNext(out var spawnerUid, out var spawnerData))
        {
            var spawnerStation = _station.GetOwningStation(spawnerUid);

            if (spawnerStation != ent.Owner)
                continue;

            if (TryComp<ApcPowerReceiverComponent>(spawnerUid, out var power) && !power.Powered)
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
