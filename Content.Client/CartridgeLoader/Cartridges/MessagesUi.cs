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
        _fragment.OnNoteRemoved += note => SendMessagesMessage(MessagesUiAction.Remove, note, userInterface);
        _fragment.OnNoteAdded += note => SendMessagesMessage(MessagesUiAction.Add, note, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessagesUiState notekeepeerState)
            return;

        _fragment?.UpdateState(notekeepeerState.Notes);
    }

    private void SendMessagesMessage(MessagesUiAction action, string note, BoundUserInterface userInterface)
    {
        var MessagesMessage = new MessagesUiMessageEvent(action, note);
        var message = new CartridgeUiMessage(MessagesMessage);
        userInterface.SendMessage(message);
    }
}
