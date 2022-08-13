using System.Linq;
using Content.Shared.Revenant;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Revenant.EntitySystems;

// TODO: Delete and replace all of this with StoreSystem once that's merged
// i'm sorry, but i'm not ultra-optimizing something that's getting deleted in a week.
// 8/7/22 -emo (bully me if this exists in the future)
public sealed partial class RevenantSystem : EntitySystem
{
    private void InitializeShop()
    {
        SubscribeLocalEvent<RevenantComponent, RevenantShopActionEvent>(OnShop);
        SubscribeLocalEvent<RevenantComponent, RevenantBuyListingMessage>(OnBuy);
    }

    private void OnShop(EntityUid uid, RevenantComponent component, RevenantShopActionEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;
        ToggleUi(component, actor.PlayerSession);
    }

    private void OnBuy(EntityUid uid, RevenantComponent component, RevenantBuyListingMessage ev)
    {
        RevenantStoreListingPrototype? targetListing = null;
        foreach (var listing in component.Listings)
        {
            if (listing.Key.ID == ev.Listing.ID)
                targetListing = listing.Key;
        }

        if (targetListing == null)
            return;
        component.Listings[targetListing] = false;

        if (component.StolenEssence < ev.Listing.Price)
            return;
        component.StolenEssence -= ev.Listing.Price;

        if (_proto.TryIndex<InstantActionPrototype>(ev.Listing.ActionId, out var action))
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

        var filterlistings = (from e in component.Listings where e.Value select e.Key).ToList();

        ui.SetState(new RevenantUpdateState(component.StolenEssence.Float(), filterlistings));
    }
}
