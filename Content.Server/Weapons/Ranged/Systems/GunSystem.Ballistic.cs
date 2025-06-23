using Content.Server.Emp;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void InitializeBallistic()
    {
        base.InitializeBallistic();

        SubscribeLocalEvent<BallisticAmmoSelfRefillerComponent, EmpPulseEvent>(OnRefillerEmpPulsed);
    }

    protected override void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        EntityUid? ent = null;

        // TODO: Combine with TakeAmmo
        if (component.Entities.Count > 0)
        {
            var existing = component.Entities[^1];
            component.Entities.RemoveAt(component.Entities.Count - 1);
            DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.Entities));

            Containers.Remove(existing, component.Container);
            EnsureShootable(existing);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
            ent = Spawn(component.Proto, coordinates);
            EnsureShootable(ent.Value);
        }

        if (ent != null)
            EjectCartridge(ent.Value);

        var cycledEvent = new GunCycledEvent();
        RaiseLocalEvent(uid, ref cycledEvent);
    }

    private void OnRefillerEmpPulsed(Entity<BallisticAmmoSelfRefillerComponent> entity, ref EmpPulseEvent args)
    {
        PauseSelfRefill(entity, args.Duration);
    }

    private void UpdateBallistic()
    {
        // Handle `BallisticAmmoSelfRefillerComponent` refills.
        var query = EntityQueryEnumerator<BallisticAmmoSelfRefillerComponent, BallisticAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var refiller, out var ammo))
        {
            var entity = new Entity<BallisticAmmoProviderComponent>(uid, ammo);
            if (!refiller.AutoRefill ||
                IsFull(entity) ||
                _timing.CurTime < refiller.NextAutoRefill)
                continue;

            var ammoEntity = Spawn(refiller.AmmoProto);
            var insertSucceeded = TryBallisticInsert(entity, ammoEntity, null, suppressInsertionSound: true);
            if (!insertSucceeded)
            {
                QueueDel(ammoEntity);
                Log.Error(
                    $"Failed to insert ammo {ammoEntity} into non-full {entity}. Is the {nameof(BallisticAmmoSelfRefillerComponent)}'s {nameof(BallisticAmmoSelfRefillerComponent.AmmoProto)} incorrect for the {nameof(BallisticAmmoProviderComponent)}'s {nameof(BallisticAmmoProviderComponent.Whitelist)}?");
                continue;
            }

            refiller.NextAutoRefill = _timing.CurTime + refiller.AutoRefillRate;
            Dirty(uid, refiller);
        }
    }
}
