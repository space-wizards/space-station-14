using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeUi : UIFragment
{
    private MessagerCartridgeUiFragment? _fragment;
    private BoundUserInterface? _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _userInterface = userInterface;
        _fragment = new MessagerCartridgeUiFragment();

        _fragment.OnSendMessage += (receiverId, content) =>
        {
            var msg = new MessagerSendMessageEvent(receiverId, content);
            _userInterface?.SendMessage(new CartridgeUiMessage(msg));
        };

        _fragment.OnRequestMessages += (userId) =>
        {
            var msg = new MessagerRequestMessagesEvent(userId);
            _userInterface?.SendMessage(new CartridgeUiMessage(msg));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessagerCartridgeUiState messagerState)
            return;

        _fragment?.UpdateState(messagerState.Status, messagerState.Users, messagerState.Messages);
    }
}
