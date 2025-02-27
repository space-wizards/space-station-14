using Content.Server.Power.Components;
using Content.Shared.Delivery;
using Content.Shared.EntityTable;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Delivery;

/// <summary>
/// If you're reading this you're gay but server side
/// </summary>
public sealed partial class DeliverySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    private void InitializeSpawning()
    {
        SubscribeLocalEvent<CargoDeliveryDataComponent, MapInitEvent>(OnDataMapInit);

        Log.Debug("Initialized");
    }

    private void OnDataMapInit(Entity<CargoDeliveryDataComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDelivery = TimeSpan.Zero;
    }

    private void SpawnDelivery(Entity<DeliverySpawnerComponent?> ent, int amount)
    {
        Log.Debug("SpawnDelivery ran");
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        Log.Debug("SpawnDelivery resolved");
        var spawns = _entityTable.GetSpawns(ent.Comp.Table);
        var coords = Transform(ent).Coordinates;

        while (amount > 0)
        {
            Log.Debug(amount.ToString());
            foreach (var id in spawns)
            {
                Spawn(id, coords);
                Log.Debug("Spawning");
            }

            amount--;
        }
    }

    private void SpawnStationDeliveries(Entity<CargoDeliveryDataComponent> ent)
    {
        Log.Debug("Attempting to spawn deliveries");
        var spawners = GetValidSpawners(ent);

        if (spawners.Count == 0)
            return;

        if (!ent.Comp.DistributeRandomly)
        {
            foreach (var spawner in spawners)
            {
                Log.Debug("Attempting to spawn deliveries at spawner");
                SpawnDelivery(spawner, ent.Comp.DeliveryCount);
            }
        }
        else
        {
            var deliverySpawnAmount = ent.Comp.DeliveryCount;
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

            Log.Debug("Added valid spawner");
            validSpawners.Add(spawnerUid);
        }
        Log.Debug("Found " + validSpawners.Count + " spawners");
        return validSpawners;
    }

    private void UpdateSpawner(float frameTime)
    {
        var dataQuery = EntityQueryEnumerator<CargoDeliveryDataComponent>();
        var curTime = _timing.CurTime;

        while (dataQuery.MoveNext(out var uid, out var deliveryData))
        {
            if (deliveryData.NextDelivery < curTime)
            {
                deliveryData.NextDelivery = curTime + deliveryData.DeliveryCooldown;
                SpawnStationDeliveries((uid, deliveryData));
            }
        }
    }
}
