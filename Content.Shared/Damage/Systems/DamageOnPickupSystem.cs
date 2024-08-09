using Content.Shared.Damage.Components;
using Robust.Shared.Containers;
using Content.Shared.Inventory;
using Robust.Shared.Audio.Systems;
using Content.Shared.Throwing;
using Content.Shared.Damage;
using Robust.Shared.Random;
using Content.Shared.Popups;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnPickupSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnPickupComponent, ContainerGettingInsertedAttemptEvent>(OnHandPickup);
    }

    private void OnHandPickup(Entity<DamageOnPickupComponent> entity, ref ContainerGettingInsertedAttemptEvent args)
    {
        var user = args.Container.Owner;

        // Verify that the entity has a gloves container slot. This returns is false, since the entity is likely a bag.
        // However, entities like monkeys and kobolds have a hand slot, but no gloves slot.
        if (_inventory.TryGetSlotContainer(user, "gloves", out var containerSlot, out var slotDefinition))
        {
            if (TryComp<DamageOnPickupImmuneComponent>(containerSlot.ContainedEntity, out var immunity))
            {
                return;
            }
        }
        else
        {
            return;
        }

        args.Cancel();
        _audio.PlayPvs(entity.Comp.Sound, entity);
        _transform.SetCoordinates(entity, Transform(user).Coordinates);
        _transform.AttachToGridOrMap(entity);
        _throwing.TryThrow(entity, _random.NextVector2(), recoil: false);
        var tookDamage = _damageableSystem.TryChangeDamage(user, entity.Comp.Damage, origin: user);
        string pickupResponse = Loc.GetString("damage-onpickup-entity", ("entity", entity.ToString()));
        _popupSystem.PopupEntity(pickupResponse, entity, user);
    }
}
