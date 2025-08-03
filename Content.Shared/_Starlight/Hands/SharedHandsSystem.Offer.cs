using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    private ProtoId<AlertPrototype> OfferAlert = "Offer";

    private void InitializeOffer()
    {
        SubscribeLocalEvent<HandsComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<ItemComponent, GotUnequippedHandEvent>(OnUnequipHand);

        if (_net.IsServer) // Wow! This is bullshit but im not making a script to ServerSide.
            SubscribeLocalEvent<ItemComponent, DroppedEvent>(OnDropped);

        SubscribeLocalEvent<HandsComponent, OfferItemAlertEvent>(OnOfferItemAlertEvent);

        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<Verb>>(OfferItemVerb);
    }

    private void OnOfferItemAlertEvent(Entity<HandsComponent> ent, ref OfferItemAlertEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        AcceptOffer(ent.Owner, ent.Comp);

        args.Handled = true;
    }

    private void AcceptOffer(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (TryComp<HandsComponent>(handsComp.OfferTarget, out var targetHands))
            targetHands.ReceivingOffer = true;

        if (handsComp.OfferItem is not null)
        {
            if (!TryPickupAnyHand(uid, handsComp.OfferItem.Value))
            {
                _popupSystem.PopupEntity(Loc.GetString("hands-full"), uid, uid);
                return;
            }

            if (handsComp.OfferTarget is not null)
            {
                _popupSystem.PopupEntity(Loc.GetString("offered-target", ("item", handsComp.OfferItem), ("user", handsComp.OfferTarget.Value)), handsComp.OfferTarget.Value, uid);
                _popupSystem.PopupEntity(Loc.GetString("offered", ("item", handsComp.OfferItem)), handsComp.OfferTarget.Value, handsComp.OfferTarget.Value);
                EndOffer(handsComp.OfferTarget.Value, targetHands ?? null, false);
            }
        }

        EndOffer(uid, handsComp, false);
    }

    private void OfferItemVerb(EntityUid uid, HandsComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.User == args.Target
            || args.Using is null || HasComp<UnremoveableComponent>(args.Using) || HasComp<VirtualItemComponent>(args.Using)
            || component.Offering || component.ReceivingOffer
            || !TryComp<HandsComponent>(args.User, out var targetHands)
            || targetHands.Offering || targetHands.ReceivingOffer)
            return;

        args.Verbs.Add(new Verb()
        {
            Act = () => OfferItem(args.User, uid, targetHands, component),
            DoContactInteraction = true,
            Text = Loc.GetString("offer", ("item", args.Using)),
            IconEntity = GetNetEntity(args.Using)
        });
    }

    /// <summary>
    /// Offer the ActiveItem to the target (if they have a HandsComponent)
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="target"></param>
    /// <param name="handsComp"></param>
    /// <param name="targetHands"></param>
    public void OfferItem(EntityUid uid, EntityUid target, HandsComponent? handsComp = null, HandsComponent? targetHands = null)
    {
        if (!Resolve(uid, ref handsComp, false)
            || !Resolve(target, ref targetHands, false)
            || !TryGetActiveItem(uid, out var helditem))
            return;

        handsComp.OfferItem = helditem;
        targetHands.OfferItem = helditem;

        handsComp.Offering = true;
        handsComp.OfferTarget = target;

        targetHands.ReceivingOffer = true;
        targetHands.OfferTarget = uid;

        _alertsSystem.ShowAlert(target, OfferAlert);

        if (_net.IsServer)
        {
            _popupSystem.PopupEntity(Loc.GetString("offering", ("item", helditem), ("target", target)), uid, uid);
            _popupSystem.PopupEntity(Loc.GetString("offering-target", ("item", helditem), ("user", uid)), uid, target);
        }
    }

    /// <summary>
    /// End the Offer, this can be used to cancel or end the process.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="handsComp"></param>
    public void EndOffer(EntityUid uid, HandsComponent? handsComp = null, bool? popups = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (popups ?? true)
        {
            if (handsComp.OfferItem is not null)
            {
                if (handsComp.ReceivingOffer)
                {
                    if (handsComp.OfferTarget is not null)
                        _popupSystem.PopupEntity(Loc.GetString("unoffer-target", ("item", handsComp.OfferItem), ("user", handsComp.OfferTarget)), handsComp.OfferTarget.Value, uid);
                }
                else
                    _popupSystem.PopupEntity(Loc.GetString("unoffer", ("item", handsComp.OfferItem)), uid, uid);
            }
        }

        _alertsSystem.ClearAlert(uid, OfferAlert);

        handsComp.Offering = false;
        handsComp.ReceivingOffer = false;
        handsComp.OfferItem = null;
        handsComp.OfferTarget = null;
    }

    private void OnMove(EntityUid uid, HandsComponent handsComp, MoveEvent args)
    {
        if (handsComp.OfferTarget is null || TransformSystem.InRange(args.NewPosition, Transform(handsComp.OfferTarget.Value).Coordinates, 2f))
            return;

        EndOffer(handsComp.OfferTarget.Value);
        EndOffer(uid, handsComp);
    }

    private void OnDropped(EntityUid uid, ItemComponent item, DroppedEvent args)
    {
        if (!TryComp<HandsComponent>(args.User, out var handComp) || !handComp.Offering || handComp.OfferItem != uid)
            return;

        if (handComp.OfferTarget is not null)
            EndOffer(handComp.OfferTarget.Value);

        EndOffer(args.User, handComp);
    }

    private void OnUnequipHand(EntityUid uid, ItemComponent item, GotUnequippedHandEvent args)
    {
        if (_net.IsClient || !TryComp<HandsComponent>(args.User, out var handComp) || !handComp.Offering || handComp.OfferItem != uid)
            return;

        if (handComp.Offering && handComp.ReceivingOffer) // This check is to make sure OnUnequipHand when AcceptOffer, Silly but works!
            return;

        if (handComp.OfferTarget is not null)
            EndOffer(handComp.OfferTarget.Value);

        EndOffer(args.User, handComp);
    }
}
