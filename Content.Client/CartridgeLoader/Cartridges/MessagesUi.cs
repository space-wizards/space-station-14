using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessagesUi : UIFragment
{
    private MessagesUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessagesUiFragment();
        _fragment.OnMessageSent += note => SendMessagesMessage(MessagesUiAction.Send, note, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessagesUiState messagesState)
            return;

        _fragment?.UpdateState(messagesState.Contents, messagesState.Mode);
    }

    private void SendMessagesMessage(MessagesUiAction action, MessagesMessageData message, BoundUserInterface userInterface)
    {
        var MessagesMessage = new MessagesUiMessageEvent(action, note);
        var message = new CartridgeUiMessage(MessagesMessage);
        userInterface.SendMessage(message);
    }
}
