using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class PdaMessengerUi : UIFragment
{
    private PdaMessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new PdaMessengerUiFragment();
        _fragment.OnSend += (recipientId, text) => Send(userInterface, new PdaMessengerSendMessage(recipientId, text));
        _fragment.OnOpenConversation += id => Send(userInterface, new PdaMessengerOpenConversation(id));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is PdaMessengerUiState messengerState)
            _fragment?.UpdateState(messengerState);
    }

    // The messenger's logic is server-only, so messages aren't predicted.
    private static void Send(BoundUserInterface userInterface, IPdaMessengerUiMessagePayload payload)
    {
        userInterface.SendMessage(new CartridgeUiMessage(new PdaMessengerUiMessageEvent(payload)));
    }
}
