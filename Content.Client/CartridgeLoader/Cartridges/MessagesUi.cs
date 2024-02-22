using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessagesUi : UIFragment
{
    private MessagesUiFragment? _fragment;

    //private ISawmill _sawmill = default!;

     public void Initialize()
    {
       ;// _sawmill = Logger.GetSawmill("chat");
    }

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessagesUiFragment();
        _fragment.OnMessageSent += note => SendMessagesMessage(MessagesUiAction.Send, note, userInterface);
        _fragment.OnButtonPressed += userUid => SendMessagesMessage(MessagesUiAction.ChangeChat, userUid, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        //_sawmill?.Debug("UI updating state");
        if (state is not MessagesUiState messagesState)
            return;

        _fragment?.UpdateState(messagesState.Mode, messagesState.Contents, messagesState.Name);
    }

    private void SendMessagesMessage(MessagesUiAction action, string? parameter, BoundUserInterface userInterface)
    {
        //_sawmill?.Debug("Ui making event");
        var MessagesMessage = new MessagesUiMessageEvent(action, parameter);
        //_sawmill?.Debug("Ui making message");
        var message = new CartridgeUiMessage(MessagesMessage);
        //_sawmill?.Debug("Ui sending message");
        userInterface.SendMessage(message);
    }
}
