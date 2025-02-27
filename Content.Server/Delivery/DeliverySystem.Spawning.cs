using Content.Server.Cargo.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Interaction.Events;
using Content.Shared.StationRecords;
using Robust.Shared.Timing;

namespace Content.Server.Delivery;

/// <summary>
/// If you're reading this you're gay but server side
/// </summary>
public sealed partial class DeliverySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeSpawning()
    {
        SubscribeLocalEvent<CargoDeliveryDataComponent, MapInitEvent>(OnDataMapInit);

        Log.Debug("Initialized");
    }

    private void OnDataMapInit(Entity<CargoDeliveryDataComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDelivery = TimeSpan.Zero;
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
                Log.Debug("Delivery spawn rn isnt that epic");
            }
        }
    }
}
