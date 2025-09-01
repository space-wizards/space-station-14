using Content.Shared.Inventory.Events;
using Content.Shared.DoAfter;
using Content.Shared.Clothing.Components;
using Robust.Shared.Localization;

namespace Content.Shared._Starlight.IdClothingBlocker;

public abstract class SharedIdClothingBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdClothingBlockerComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<IdClothingBlockerComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<IdClothingBlockerComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<IdClothingBlockerComponent, DoAfterAttemptEvent<ClothingUnequipDoAfterEvent>>(OnUnequipDoAfterAttempt);
    }

    protected virtual void OnUnequipAttempt(EntityUid uid, IdClothingBlockerComponent component,
        BeingUnequippedAttemptEvent args)
    {
        var wearerHasAccess = HasJobAccess(args.Unequipee, component);
        if (wearerHasAccess)
            return;

        if (args.UnEquipTarget == args.Unequipee)
        {
            args.Cancel();
        }
    }

    protected virtual void OnUnequipDoAfterAttempt(EntityUid uid, IdClothingBlockerComponent component,
        DoAfterAttemptEvent<ClothingUnequipDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target == null)
            return;

        var wearerHasAccess = HasJobAccess(args.DoAfter.Args.Target.Value, component);

        if (wearerHasAccess)
            return;

        args.Cancel();
        PopupClient(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), uid);
    }
    
    protected virtual bool HasJobAccess(EntityUid wearer, IdClothingBlockerComponent component)
    {
        return !component.IsBlocked;
    }

    private void OnGotEquipped(EntityUid uid, IdClothingBlockerComponent component, GotEquippedEvent args)
    {
        var wearerHasAccess = HasJobAccess(args.Equipee, component);

        if (wearerHasAccess)
            return;

        OnUnauthorizedAccess(uid, component, args.Equipee);
    }

    protected virtual void OnUnauthorizedAccess(EntityUid clothingUid, IdClothingBlockerComponent component,
        EntityUid wearer)
    {
    }

    private void OnGotUnequipped(EntityUid uid, IdClothingBlockerComponent component, GotUnequippedEvent args)
    {
        if (EntityManager.EntityExists(args.Equipee) &&
            EntityManager.HasComponent<IdClothingFrozenComponent>(args.Equipee))
        {
            EntityManager.RemoveComponent<IdClothingFrozenComponent>(args.Equipee);
        }
    }

    protected virtual void PopupClient(string message, EntityUid uid, EntityUid? target = null)
    {
    }
}