using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessengerCartridgeUi : UIFragment
{
    private MessengerCartridgeUiFragment? _fragment;
    private BoundUserInterface? _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _userInterface = userInterface;
        _fragment = new MessengerCartridgeUiFragment();

        _fragment.OnSendMessage += (receiverId, content) =>
        {
            var msg = new MessengerSendMessageEvent(receiverId, content);
            _userInterface?.SendMessage(new CartridgeUiMessage(msg));
        };

        _fragment.OnRequestMessages += (userId) =>
        {
            var msg = new MessengerRequestMessagesEvent(userId);
            _userInterface?.SendMessage(new CartridgeUiMessage(msg));
        };

        _fragment.OnTyping += () =>
        {
            var msg = new MessengerTypingEvent();
            _userInterface?.SendMessage(new CartridgeUiMessage(msg));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerCartridgeUiState messengerState)
            return;

        _fragment?.UpdateState(messengerState.Status, messengerState.Users, messengerState.Messages);
    }
}
