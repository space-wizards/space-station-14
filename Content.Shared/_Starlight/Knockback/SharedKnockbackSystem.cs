using System.Numerics;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Starlight.Knockback;
public abstract partial class SharedKnockbackSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<KnockbackByUserTagComponent, OnNonEmptyGunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<KnockbackByUserTagComponent> ent, ref OnNonEmptyGunShotEvent args)
    {
        //make sure the ammo is shootable
        foreach (var ammo in args.Ammo)
        {
            if (TryComp<HitScanCartridgeAmmoComponent>(ammo.Uid, out var hitscanCartridge))
            {
                //check if its spent
                if (hitscanCartridge.Spent)
                {
                    return;
                }
            }

            if (TryComp<CartridgeAmmoComponent>(ammo.Uid, out var cartridge))
            {
                //check if its spent
                if (cartridge.Spent)
                {
                    return;
                }
            }
        }

        //check for tags
        if (!_tagSystem.HasAllTags(args.User, ent.Comp.Contains) || _tagSystem.HasAnyTag(args.User, ent.Comp.DoestContain))
        {
            //get the gun component
            if (TryComp<GunComponent>(ent, out var gunComponent))
            {
                var toCoordinates = gunComponent.ShootCoordinates;

                if (toCoordinates == null)
                    return;

                float knockback = ent.Comp.Knockback;
                //If we have no slips, cut the knockback in half
                if (CheckForNoSlips(args.User))
                {
                    knockback *= 0.5f;
                }

                if (knockback == 0.0f)
                    return;

                //make a clone, not a reference
                Vector2 modifiedCoords = toCoordinates.Value.Position;
                //flip the direction
                if (knockback > 0)
                    modifiedCoords = -modifiedCoords;

                //absolute knockback now
                knockback = Math.Abs(knockback);
                //normalize them
                modifiedCoords = Vector2.Normalize(modifiedCoords);
                //multiply by the knockback value
                modifiedCoords *= knockback;
                //set the new coordinates
                var flippedDirection = new EntityCoordinates(args.User, modifiedCoords);

                _throwing.TryThrow(args.User, flippedDirection, knockback * 5, args.User, 0, doSpin: false, compensateFriction: true);

                //deal stamina damage
                if (TryComp<StaminaComponent>(args.User, out var stamina))
                {
                    _stamina.TakeStaminaDamage(args.User, knockback * ent.Comp.StaminaMultiplier, component: stamina);
                }
            }
        }
    }

    private bool CheckForNoSlips(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out NoSlipComponent? flashImmunityComponent))
        {
            return true;
        }

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            //get all worn items
            var slots = _inventory.GetSlotEnumerator((uid, inventoryComp), SlotFlags.WITHOUT_POCKET);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null && EntityManager.TryGetComponent(slot.ContainedEntity, out NoSlipComponent? wornNoSlipComponent))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
