using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    private ProtoId<AlertPrototype> OfferAlert = "Offer";

    private void InitializeOffer()
    {
        SubscribeLocalEvent<HandsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<InnateVerb>>(OfferItemVerb);
        SubscribeLocalEvent<HandsComponent, OfferItemAlertEvent>(OnOfferItemAlertEvent);
    }

    private void OnOfferItemAlertEvent(Entity<HandsComponent> ent, ref OfferItemAlertEvent args)
    {
        if (args.Handled)
            return;

        AcceptOffer(ent.Owner, ent.Comp);

        args.Handled = true;
    }

    private void AcceptOffer(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (handsComp.OfferItem is not null)
        {
            if (!TryPickupAnyHand(uid, handsComp.OfferItem.Value))
            {
                _popupSystem.PopupEntity(Loc.GetString("hands-full"), uid);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("offered", ("item", handsComp.OfferItem)), uid);

            if (handsComp.OfferTarget is not null)
                _popupSystem.PopupEntity(Loc.GetString("offered-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);
        }

        UnOfferItem(uid, handsComp);
    }

    private void OfferItemVerb(EntityUid uid, HandsComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess
            || args.User == args.Target
            || HasComp<HandsComponent>(args.Target))
            return;

        InnateVerb verbOfferItem = new()
        {
            Act = () => OfferItem(args.User, args.Target, component),
            Text = Loc.GetString("alerts-offer-name")
        };
        args.Verbs.Add(verbOfferItem);
    }
    // public void TryOfferingItem(EntityUid uid, HandsComponent? handsComp = null)
    // {
    //     if (!Resolve(uid, ref handsComp))
    //         return;

    //     if (!_actionBlocker.CanInteract(uid, null)
    //         || handsComp.ActiveHand is null)
    //         return;

    //     handsComp.OfferItem = handsComp.ActiveHandEntity;

    //     if (!handsComp.Offering)
    //     {
    //         if (handsComp.OfferItem is null)
    //             return;
    //         if (HasComp<UnremoveableComponent>(handsComp.OfferItem))
    //             return;

    //         if (handsComp.OfferHand is null || handsComp.OfferTarget is null)
    //         {
    //             handsComp.Offering = true;
    //             handsComp.OfferHand = handsComp.ActiveHand.Name;
    //             return;
    //         }
    //     }

    //     if (handsComp.OfferTarget is not null)
    //         UnOfferItem(handsComp.OfferTarget.Value);
    // }

    private bool OfferItem(EntityUid uid, EntityUid target, HandsComponent? handsComp = null, HandsComponent? targetHands = null)
    {
        if (!Resolve(uid, ref handsComp, false)
            || !Resolve(target, ref targetHands, false))
            return false;

        if (target == uid
            || targetHands.ReceivingOffer
            || !handsComp.Offering
            || (handsComp.ReceivingOffer && handsComp.OfferTarget != uid))
            return false;

        targetHands.ReceivingOffer = true;
        targetHands.OfferTarget = target;
        targetHands.OfferItem = handsComp.OfferItem;

        handsComp.OfferTarget = uid;
        handsComp.Offering = false;

        if (handsComp.OfferItem is not null)
        {
            _popupSystem.PopupEntity(Loc.GetString("offering", ("item", handsComp.OfferItem), ("target", handsComp.OfferTarget)), uid);
            _popupSystem.PopupEntity(Loc.GetString("offering-target", ("item", handsComp.OfferItem), ("user", uid)), target);
        }

        return true;
    }

    public void UnOfferItem(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (handsComp.OfferTarget is not null && TryComp<HandsComponent>(handsComp.OfferTarget, out var targetHands))
        {
            if (handsComp.OfferItem is not null)
            {
                _popupSystem.PopupEntity(Loc.GetString("unoffer", ("item", handsComp.OfferItem)), uid);
                _popupSystem.PopupEntity(Loc.GetString("unoffer-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);
            }
            else if (targetHands.OfferItem is not null)
            {
                _popupSystem.PopupEntity(Loc.GetString("unoffer", ("item", targetHands.OfferItem)), uid);
                _popupSystem.PopupEntity(Loc.GetString("unoffer-target", ("item", targetHands.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);
            }

            targetHands.Offering = false;
            targetHands.ReceivingOffer = false;
            targetHands.OfferHand = null;
            targetHands.OfferItem = null;
            targetHands.OfferTarget = null;
        }

        handsComp.Offering = false;
        handsComp.ReceivingOffer = false;
        handsComp.OfferHand = null;
        handsComp.OfferItem = null;
        handsComp.OfferTarget = null;
    }

    public void UnReviceItem(EntityUid uid, EntityUid target, HandsComponent? handsComp = null, HandsComponent? targetHands = null)
    {
        if (!Resolve(uid, ref handsComp, false)
            || !Resolve(target, ref targetHands, false))
            return;

        if (handsComp.ActiveHand is null || handsComp.OfferTarget is null)
            return;

        if (handsComp.OfferItem is not null)
        {
            _popupSystem.PopupEntity(Loc.GetString("unoffer", ("item", handsComp.OfferItem)), uid);
            _popupSystem.PopupEntity(Loc.GetString("unoffer-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);
        }

        if (!handsComp.ReceivingOffer)
        {
            handsComp.OfferTarget = null;
            targetHands.OfferTarget = null;
        }

        handsComp.OfferItem = null;
        targetHands.OfferItem = null;
        handsComp.ReceivingOffer = false;
    }

    private void OnMove(EntityUid uid, HandsComponent handsComp, MoveEvent args)
    {
        if (handsComp.OfferTarget is null || TransformSystem.InRange(args.NewPosition, Transform(handsComp.OfferTarget.Value).Coordinates, 2f))
            return;

        UnOfferItem(uid, handsComp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var uid, out var handsComp))
        {
            if (handsComp.ReceivingOffer)
                _alertsSystem.ShowAlert(uid, OfferAlert);
            else
                _alertsSystem.ClearAlert(uid, OfferAlert);

            if (handsComp.ActiveHand is null)
                continue;

            if (handsComp.OfferHand is not null && handsComp.Hands[handsComp.OfferHand].HeldEntity is null)
            {
                if (handsComp.OfferTarget is not null)
                {
                    UnReviceItem(handsComp.OfferTarget.Value, uid, handsComp);
                    handsComp.Offering = false;
                }
                else
                    UnOfferItem(uid, handsComp);
            }
        }
    }
}
