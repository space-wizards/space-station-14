using Content.Shared.Revenant;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    public void InitializeShop()
    {
        SubscribeLocalEvent<RevenantComponent, RevenantShopActionEvent>(OnShop);
        SubscribeLocalEvent<RevenantComponent, RevenantBuyListingMessage>(OnBuy);
    }

    public void OnShop(EntityUid uid, RevenantComponent component, RevenantShopActionEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;
        ToggleUi(component, actor.PlayerSession);
    }

    private void OnBuy(EntityUid uid, RevenantComponent component, RevenantBuyListingMessage ev)
    {
        if (component.StolenEssence < ev.Listing.Price)
            return;
        component.StolenEssence -= ev.Listing.Price;

        if (!_proto.TryIndex<InstantActionPrototype>(ev.Listing.ActionId, out var action))
            return;

        for (var i = 0; i < component.Listings.Count; i++)
        {
            var ent = component.Listings[i];

            if (ent.ID == ev.Listing.ID)
            {
                component.Listings.Remove(ent);
                i--;
            }
        }

        _action.AddAction(uid, new InstantAction(action), null);

        UpdateUserInterface(component);
    }

    public void ToggleUi(RevenantComponent component, IPlayerSession session)
    {
        var ui = component.Owner.GetUIOrNull(RevenantUiKey.Key);
        ui?.Toggle(session);

        UpdateUserInterface(component);
    }

    private void UpdateUserInterface(RevenantComponent component)
    {
        var ui = component.Owner.GetUIOrNull(RevenantUiKey.Key);
        if (ui == null)
            return;

        ui.SetState(new RevenantUpdateState(component.StolenEssence, component.Listings));
    }
}
