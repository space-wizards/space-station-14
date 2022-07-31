using Content.Shared.Traitor.Uplink;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Store.Ui;

[UsedImplicitly]
public sealed class StoreBoundUserInterface : BoundUserInterface
{
    private StoreMenu? _menu;

    public StoreBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        _menu = new StoreMenu();

        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendMessage(new UplinkBuyListingMessage(listing.ItemId));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            //_menu.CurrentFilterCategory = category;
            SendMessage(new UplinkRequestUpdateInterfaceMessage());
        };

        _menu.OnWithdrawAttempt += (tc) =>
        {
            SendMessage(new UplinkTryWithdrawTC(tc));
        };
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        switch (state)
        {
            case StoreUpdateState msg:
                _menu.PopulateStoreCategoryButtons(msg.Listings);
                //_menu.UpdateAccount(msg.Account);
                //_menu.UpdateListing(msg.Listings);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Close();
        _menu?.Dispose();
    }
}
