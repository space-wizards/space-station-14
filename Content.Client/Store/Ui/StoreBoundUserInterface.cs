using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using System.Linq;
using System.Threading;
using Serilog;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Store.Ui;

[UsedImplicitly]
public sealed class StoreBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StoreMenu? _menu;

    [ViewVariables]
    private string _windowName = Loc.GetString("store-ui-default-title");

    public StoreBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _menu = new StoreMenu(_windowName);

        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendMessage(new StoreBuyListingMessage(listing));
        };

        _menu.OnCategoryButtonPressed += (_, category) =>
        {
            _menu.CurrentCategory = category;
            SendMessage(new StoreRequestUpdateInterfaceMessage());
        };

        _menu.OnWithdrawAttempt += (_, type, amount) =>
        {
            SendMessage(new StoreRequestWithdrawMessage(type, amount));
        };

        _menu.OnRefreshButtonPressed += (_) =>
        {
            SendMessage(new StoreRequestUpdateInterfaceMessage());
        };

        _menu.OnRefundAttempt += (_) =>
        {
            SendMessage(new StoreRequestRefundMessage());
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
                _menu.UpdateBalance(msg.Balance);
                _menu.PopulateStoreCategoryButtons(msg.Listings);

                _menu.UpdateListing(msg.Listings.ToList());
                _menu.SetFooterVisibility(msg.ShowFooter);
                _menu.UpdateRefund(msg.AllowRefund);
                break;
            case StoreInitializeState msg:
                _windowName = msg.Name;
                if (_menu != null && _menu.Window != null)
                {
                    _menu.Window.Title = msg.Name;
                }
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
