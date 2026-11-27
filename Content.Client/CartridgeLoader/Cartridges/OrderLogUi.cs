using Content.Client.UserInterface.Fragments;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class OrderLogUi : UIFragment
{
    private OrderLogUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new OrderLogUiFragment();

        _fragment.OnNotificationSwitchPressed += () =>
        {
            SendOrderLogMessage(OrderLogUiAction.NotificationSwitch, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not OrderLogUiState orderLogState)
            return;

        _fragment?.UpdateState(orderLogState.Orders, orderLogState.NotificationOn);
    }

    private static void SendOrderLogMessage(OrderLogUiAction action, BoundUserInterface userInterface)
    {
        var orderLogMessage = new OrderLogUiMessageEvent(action);
        var message = new CartridgeUiMessage(orderLogMessage);
        userInterface.SendMessage(message);
    }
}
