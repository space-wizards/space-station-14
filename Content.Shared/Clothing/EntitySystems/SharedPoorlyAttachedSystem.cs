using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

public abstract partial class SharedPoorlyAttachedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoorlyAttachedComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<PoorlyAttachedComponent, InventoryRelayedEvent<FellDownEvent>>(OnWearerSlipped);
        SubscribeLocalEvent<PoorlyAttachedComponent, GetVerbsEvent<Verb>>(AddReattachVerb);
    }

    private void OnGotEquipped(Entity<PoorlyAttachedComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ResetAttachmentStrength(ent);
    }

    private void OnWearerSlipped(Entity<PoorlyAttachedComponent> ent, ref InventoryRelayedEvent<FellDownEvent> args)
    {
        ChangeAttachmentStrength(ent.AsNullable(), ent.Comp.LossPerFall);
    }

    private void AddReattachVerb(Entity<PoorlyAttachedComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanInteract)
            return;

        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if (!TryComp<ClothingComponent>(entity, out var clothing) || (clothing.InSlotFlag & clothing.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((entity, null), out var container))
            return;
        var wearer = container.Owner;

        if (!entity.Comp.OthersCanReattach && args.User != wearer)
            return;

        // We don't care if the user can access the item itself (they can't if they're not the wearer).
        // We DO care if the user can access the wearer.
        if (!_interaction.InRangeAndAccessible(args.User, wearer))
            return;

        var user = args.User;
        var target = args.Target;
        var adjustVerb = new Verb()
        {
            Text = Loc.GetString(entity.Comp.ReattachVerb),
            Act = () => Reattach((entity, null, clothing), user)
        };
        args.Verbs.Add(adjustVerb);
    }

    public void Reattach(Entity<PoorlyAttachedComponent?, ClothingComponent?> entity, EntityUid user)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return;

        var poorlyAttachedComp = entity.Comp1;
        var clothingComp = entity.Comp2;

        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if ((clothingComp.InSlotFlag & clothingComp.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((entity, null), out var container))
            return;
        var wearer = container.Owner;

        if (!poorlyAttachedComp.OthersCanReattach && user != wearer)
            return;

        var userIdentity = Identity.Entity(user, EntityManager);
        var wearerIdentity = Identity.Entity(wearer, EntityManager);

        if (user == wearer)
        {
            // "You reattach your item"
            var userMessage = Loc.GetString(poorlyAttachedComp.ReattachSelfPopupUser, ("entity", entity.Owner));
            // "Urist McHands reattaches his item"
            var othersMessage = Loc.GetString(poorlyAttachedComp.ReattachSelfPopupOthers, ("entity", entity.Owner), ("user", userIdentity));
            Popup.PopupPredicted(userMessage, othersMessage, wearer, user);
        }
        else
        {
            // "You reattach Urist McWearer's item"
            var userMessage = Loc.GetString(poorlyAttachedComp.ReattachOtherPopupUser, ("entity", entity.Owner), ("wearer", wearerIdentity));
            // "Urist McHands reattaches your item"
            var wearerMessage = Loc.GetString(poorlyAttachedComp.ReattachOtherPopupWearer, ("entity", entity.Owner), ("user", userIdentity));
            // "Urist McHands reattaches Urist McWearer's item"
            var othersMessage = Loc.GetString(poorlyAttachedComp.ReattachOtherPopupOthers, ("entity", entity.Owner), ("user", userIdentity), ("wearer", wearerIdentity));
            var othersFilter = Filter.PvsExcept(wearer, entityManager: EntityManager).RemovePlayerByAttachedEntity(user);
            Popup.PopupClient(userMessage, wearer, user);
            Popup.PopupEntity(wearerMessage, wearer, wearer);
            Popup.PopupEntity(othersMessage, wearer, othersFilter, true);
        }

        ResetAttachmentStrength((entity, poorlyAttachedComp));
    }

    public float GetAttachmentStrength(Entity<PoorlyAttachedComponent?> entity)
    {
        // If it doesn't have the component, consider it fully attached
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return 1;

        var timeSinceAttached = _timing.CurTime - entity.Comp.AttachmentTime;

        // Start with full strength (1), then subtract the total of all events
        return MathF.Max(0, 1f - entity.Comp.EventStrengthTotal - entity.Comp.LossPerSecond * (float)timeSinceAttached.TotalSeconds);
    }

    public void ChangeAttachmentStrength(Entity<PoorlyAttachedComponent?> entity, float amount, bool canDetach = true)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        entity.Comp.EventStrengthTotal += amount;

        if (canDetach && GetAttachmentStrength(entity) <= 0)
        {
            Detach((entity, entity.Comp));
        }
    }

    private void ResetAttachmentStrength(Entity<PoorlyAttachedComponent> entity)
    {
        entity.Comp.AttachmentTime = _timing.CurTime;
        entity.Comp.EventStrengthTotal = 0;
        Dirty(entity);
    }

    private void Detach(Entity<PoorlyAttachedComponent> entity)
    {
        // Make sure the item is equipped in a valid slot (not just in a pocket)
        if (!TryComp<ClothingComponent>(entity, out var clothing) || (clothing.InSlotFlag & clothing.Slots) == SlotFlags.NONE)
            return;

        // Make sure the item is actually equipped (and get the wearer's uid)
        if (!_container.TryGetContainingContainer((entity, null), out var container))
            return;
        var wearer = container.Owner;

        if (!_inventory.CanUnequip(wearer, container.ID, out _))
            return;

        Throw(entity, wearer);
    }

    protected virtual void Throw(Entity<PoorlyAttachedComponent> entity, EntityUid wearer) { }
}
