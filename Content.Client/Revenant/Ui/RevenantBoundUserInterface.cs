using Content.Shared.Revenant;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant.Ui;

[UsedImplicitly]
public sealed class RevenantBoundUserInterface : BoundUserInterface
{
    private RevenantMenu? _menu;

    public RevenantBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        _menu = new();
        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.OnListingButtonPressed += (_, listing) =>
        {
            SendMessage(new RevenantBuyListingMessage(listing));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        switch (state)
        {
            case RevenantUpdateState msg:
                _menu.UpdateEssence(msg.Essence);
                _menu.UpdateListing(msg.Listings);
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
