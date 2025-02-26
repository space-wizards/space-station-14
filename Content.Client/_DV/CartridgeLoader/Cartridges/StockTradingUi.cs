using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Client._DV.CartridgeLoader.Cartridges;

public sealed partial class StockTradingUi : UIFragment
{
    private StockTradingUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new StockTradingUiFragment();

        _fragment.OnBuyButtonPressed += (company, amount) =>
        {
            SendStockTradingUiMessage(StockTradingUiAction.Buy, company, amount, userInterface);
        };
        _fragment.OnSellButtonPressed += (company, amount) =>
        {
            SendStockTradingUiMessage(StockTradingUiAction.Sell, company, amount, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is StockTradingUiState cast)
        {
            _fragment?.UpdateState(cast);
        }
    }

    private static void SendStockTradingUiMessage(StockTradingUiAction action, int company, int amount, BoundUserInterface userInterface)
    {
        var newsMessage = new StockTradingUiMessageEvent(action, company, amount);
        var message = new CartridgeUiMessage(newsMessage);
        userInterface.SendMessage(message);
    }
}
