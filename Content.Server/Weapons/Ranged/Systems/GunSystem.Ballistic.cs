using Content.Shared.Emp;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
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
        if (!entity.Comp.AffectedByEmp)
            return;

        PauseSelfRefill(entity, args.Duration);
    }

    private void UpdateBallistic()
    {
        var query = EntityQueryEnumerator<BallisticAmmoSelfRefillerComponent, BallisticAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var refiller, out var ammo))
        {
            BallisticSelfRefillerUpdate((uid, ammo, refiller));
        }
    }

    private void BallisticSelfRefillerUpdate(
        Entity<BallisticAmmoProviderComponent, BallisticAmmoSelfRefillerComponent> entity
    )
    {
        var ammo = entity.Comp1;
        var refiller = entity.Comp2;
        if (!refiller.AutoRefill ||
            IsFull(entity) ||
            Timing.CurTime < refiller.NextAutoRefill)
            return;

        if (refiller.AmmoProto is not { } refillerAmmoProto)
        {
            // No ammo proto on the refiller, so just increment the unspawned count on the provider
            // if it has an ammo proto.
            if (ammo.Proto is null)
            {
                Log.Error(
                    $"Neither of {entity}'s {nameof(BallisticAmmoSelfRefillerComponent)}'s or {nameof(BallisticAmmoProviderComponent)}'s ammunition protos is specified. This is a configuration error as it means {nameof(BallisticAmmoSelfRefillerComponent)} cannot do anything.");
                return;
            }

            SetBallisticUnspawned(entity, ammo.UnspawnedCount + 1);
        }
        else if (ammo.Proto == refillerAmmoProto)
        {
            // The ammo proto on the refiller and the provider match. Add an unspawned ammo.
            SetBallisticUnspawned(entity, ammo.UnspawnedCount + 1);
        }
        else
        {
            // Can't use unspawned ammo, so spawn an entity and try to insert it.
            var ammoEntity = Spawn(refiller.AmmoProto);
            var insertSucceeded = TryBallisticInsert(entity, ammoEntity, null, suppressInsertionSound: true);
            if (!insertSucceeded)
            {
                QueueDel(ammoEntity);
                Log.Error(
                    $"Failed to insert ammo {ammoEntity} into non-full {entity}. This is a configuration error. Is the {nameof(BallisticAmmoSelfRefillerComponent)}'s {nameof(BallisticAmmoSelfRefillerComponent.AmmoProto)} incorrect for the {nameof(BallisticAmmoProviderComponent)}'s {nameof(BallisticAmmoProviderComponent.Whitelist)}?");
                return;
            }
        }

        refiller.NextAutoRefill = Timing.CurTime + refiller.AutoRefillRate;
        Dirty(entity);
    }
}
