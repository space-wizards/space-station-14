using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;
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
        SubscribeLocalEvent<HandsComponent, InteractUsingEvent>(OnInteractUsingEvent);
        // SubscribeLocalEvent<HandsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<HandsComponent, OfferItemAlertEvent>(OnOfferItemAlertEvent);
        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<InnateVerb>>(OfferItemVerb);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OfferItemInHand, InputCmdHandler.FromDelegate(OfferItemInHand, handle: false, outsidePrediction: false))
            .Register<SharedHandsSystem>();
    }

    private void OfferItemVerb(EntityUid uid, HandsComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess
            || args.User == args.Target)
            return;

        InnateVerb verbOfferItem = new()
        {
            Act = () => OfferItem(args.User, args.Target, component),
            Text = Loc.GetString("alerts-offer-name")
        };
        args.Verbs.Add(verbOfferItem);
    }

    private void OfferItemInHand(ICommonSession? session)
    {
        if (session?.AttachedEntity != null)
            TryOfferingItem(session.AttachedEntity.Value);
    }

    public bool TryOfferingItem(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return false;

        if (!handsComp.Offering)
        {
            UnOfferItem(uid, handsComp);
            return false;
        }

        if (_actionBlocker.CanInteract(uid, null)
                || handsComp.ActiveHand is null)
            return false;

        if (handsComp.ActiveHandEntity is null)
            return false;
        else if (HasComp<UnremoveableComponent>(handsComp.ActiveHandEntity))
            return false;

        handsComp.Offering = true;
        return true;
    }

    private void OnInteractUsingEvent(EntityUid uid, HandsComponent component, InteractUsingEvent args)
    {
        if (!OfferItem(uid, args.User, component))
            return;

        args.Handled = true;
    }

    private bool OfferItem(EntityUid uid, EntityUid target, HandsComponent? handsComp = null, HandsComponent? targetHands = null)
    {
        if (!Resolve(uid, ref handsComp, false)
            || !Resolve(target, ref targetHands, false))
            return false;

        if (target == uid
            || handsComp.Offering
            || handsComp.OfferTarget is not null
            || handsComp.OfferItem is null
            || targetHands.BeingOffered)
            return false;

        targetHands.BeingOffered = true;
        targetHands.OfferTarget = uid;
        handsComp.OfferTarget = target;

        _popupSystem.PopupEntity(Loc.GetString("offering", ("item", handsComp.OfferItem), ("target", handsComp.OfferTarget)), uid);
        _popupSystem.PopupEntity(Loc.GetString("offering-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);

        return true;
    }

    public void UnOfferItem(EntityUid uid, HandsComponent? handsComp = null, bool Popups = true)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        handsComp.Offering = false;

        if (Popups && handsComp.OfferItem is not null)
            _popupSystem.PopupEntity(Loc.GetString("unoffer", ("item", handsComp.OfferItem)), uid);

        if (handsComp.OfferTarget is not null)
        {
            if (Popups && handsComp.OfferItem is not null)
                _popupSystem.PopupEntity(Loc.GetString("unoffer-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);

            if (TryComp<HandsComponent>(handsComp.OfferTarget, out var targethands))
            {
                targethands.BeingOffered = false;
                targethands.OfferTarget = null;
            }

            handsComp.OfferTarget = null;
        }

        handsComp.OfferItem = null;
    }

    private void AcceptOffer(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false)
            || handsComp.OfferTarget is null
            || handsComp.OfferItem is null)
            return;

        if (!TryPickupAnyHand(handsComp.OfferTarget.Value, handsComp.OfferItem.Value))
        {
            _popupSystem.PopupEntity(Loc.GetString("hands-full"), uid);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("offered", ("item", handsComp.OfferItem), ("target", handsComp.OfferTarget)), uid);
        _popupSystem.PopupEntity(Loc.GetString("offered-target", ("item", handsComp.OfferItem), ("user", uid)), handsComp.OfferTarget.Value);

        UnOfferItem(uid, handsComp, false);
    }

    // private void OnMove(EntityUid uid, HandsComponent handsComp, MoveEvent args)
    // {
    //     UnOfferItem(uid, handsComp);
    // }

    private void OnOfferItemAlertEvent(Entity<HandsComponent> ent, ref OfferItemAlertEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.OfferTarget is not null)
            AcceptOffer(ent.Comp.OfferTarget.Value);

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var uid, out var handsComp))
        {
            if (handsComp.BeingOffered)
                _alertsSystem.ShowAlert(uid, OfferAlert);
            else
                _alertsSystem.ClearAlert(uid, OfferAlert);

            if (handsComp.ActiveHand is null
                || !handsComp.Offering)
                continue;

            if (handsComp.ActiveHandEntity != handsComp.OfferItem)
            {
                // UnOfferItem(uid, handsComp);
            }
        }
    }
}
