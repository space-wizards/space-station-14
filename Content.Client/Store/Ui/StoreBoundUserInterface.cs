using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.Client.Store.Ui;

[UsedImplicitly]
public sealed class StoreBoundUserInterface : BoundUserInterface
{
    private StoreMenu? _menu;

    private string _windowName = Loc.GetString("store-ui-default-title");

    public StoreBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        _menu = new StoreMenu(_windowName);

        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            if (_menu.CurrentBuyer != null)
              SendMessage(new StoreBuyListingMessage(_menu.CurrentBuyer.Value, listing));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            _menu.CurrentCategory = category;
            if (_menu.CurrentBuyer != null)
                SendMessage(new StoreRequestUpdateInterfaceMessage(_menu.CurrentBuyer.Value));
        };

        _menu.OnWithdrawAttempt += (_, type, amount) =>
        {
            if (_menu.CurrentBuyer != null)
                SendMessage(new StoreRequestWithdrawMessage(_menu.CurrentBuyer.Value, type, amount));
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
                if (msg.Buyer != null)
                    _menu.CurrentBuyer = msg.Buyer;
                _menu.UpdateBalance(msg.Balance);
                _menu.PopulateStoreCategoryButtons(msg.Listings);
                _menu.UpdateListing(msg.Listings.ToList());
                break;
            case StoreInitializeState msg:
                _windowName = msg.Name;
                if (_menu != null && _menu.Window != null)
                    _menu.Window.Title = msg.Name;
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
