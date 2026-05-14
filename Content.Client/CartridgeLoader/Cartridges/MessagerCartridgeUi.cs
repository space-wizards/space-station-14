using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeUi : UIFragment
{
    private MessagerCartridgeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessagerCartridgeUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessagerCartridgeUiState messagerState)
            return;

        _fragment?.UpdateState(messagerState.Status);
    }
}
