using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class PoorlyAttachedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoorlyAttachedComponent, GetVerbsEvent<Verb>>(AddReattachVerb);
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

        // We don't care if the user can access the item itself (they can't, since it's in the wearer's inventory).
        // We DO care if the user can access the wearer.
        if (!_interaction.InRangeAndAccessible(args.User, container.Owner))
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

        var userIdentity = Identity.Entity(user, EntityManager);
        var wearerIdentity = Identity.Entity(wearer, EntityManager);

        if (user == wearer)
        {
            // "You reattach your item"
            var userMessage = Loc.GetString(poorlyAttachedComp.ReattachSelfPopupUser, ("entity", entity.Owner));
            // "Urist McHands reattaches his item"
            var othersMessage = Loc.GetString(poorlyAttachedComp.ReattachSelfPopupOthers, ("entity", entity.Owner), ("user", userIdentity));
            _popup.PopupPredicted(userMessage, othersMessage, wearer, user);
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
            _popup.PopupClient(userMessage, wearer, user);
            _popup.PopupEntity(wearerMessage, wearer, wearer);
            _popup.PopupEntity(othersMessage, wearer, othersFilter, true);
        }
    }
}
