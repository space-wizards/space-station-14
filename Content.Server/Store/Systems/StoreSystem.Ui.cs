using Content.Server.Store.Components;
using Content.Server.UserInterface;
using Content.Shared.Store;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.Store.Systems;

public sealed partial class StoreSystem : EntitySystem
{
    public void ToggleUi(EntityUid user, StoreComponent component)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
        ui?.Toggle(actor.PlayerSession);

        UpdateUserInterface(user, component, ui);
    }

    public void UpdateUserInterface(EntityUid user, StoreComponent component, BoundUserInterface? ui = null)
    {
        if (ui == null)
        {
            ui = component.Owner.GetUIOrNull(StoreUiKey.Key);
            if (ui == null)
            {
                Logger.Error("No Ui key.");
                return;
            }
        }

        //this is the person who will be passed into logic for all listing filtering.
        var buyer = user;
        if (component.AccountOwner != null) //if we have one stored, then use that instead
            buyer = component.AccountOwner.Value;

        var listings = GetAvailableListings(buyer, component).ToHashSet();

        var state = new StoreUpdateState(buyer, component.Currency, listings, component.Categories);
        ui.SetState(state);
    }
}
