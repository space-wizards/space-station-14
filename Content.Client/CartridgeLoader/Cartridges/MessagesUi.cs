using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessagesUi : UIFragment
{
    private MessagesUiFragment _fragment;



    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessagesUiFragment();
        _fragment.OnMessageSent += note => SendMessagesMessage(MessagesUiAction.Send, note, null, userInterface);
        _fragment.OnButtonPressed += userUid => SendMessagesMessage(MessagesUiAction.ChangeChat, null, userUid, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessagesUiState messagesState)
            return;

        _fragment?.UpdateState(messagesState.Mode, messagesState.Contents, messagesState.Name);
    }

    private void SendMessagesMessage(MessagesUiAction action, string? stringInput, int? uidInput, BoundUserInterface userInterface)
    {
        var MessagesMessage = new MessagesUiMessageEvent(action, stringInput, uidInput);
        var message = new CartridgeUiMessage(MessagesMessage);
        userInterface.SendMessage(message);
    }
}
