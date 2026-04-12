using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessengerUiFragment();

        _fragment.OnContactSelected += name =>
        {
            var msg = new MessengerUiMessageEvent(MessengerUiAction.OpenChat, recipientName: name);
            userInterface.SendMessage(new CartridgeUiMessage(msg));
        };

        _fragment.OnBackPressed += () =>
        {
            var msg = new MessengerUiMessageEvent(MessengerUiAction.Back);
            userInterface.SendMessage(new CartridgeUiMessage(msg));
        };

        _fragment.OnMessageSent += (recipient, content) =>
        {
            var msg = new MessengerUiMessageEvent(MessengerUiAction.Send, recipientName: recipient, messageContent: content);
            userInterface.SendMessage(new CartridgeUiMessage(msg));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerUiState messengerState)
            return;

        _fragment?.UpdateState(messengerState);
    }
}