using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class SpaceVendorsUi : UIFragment
{
    private SpaceVendorsUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new SpaceVendorsUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not SpaceVendorsUiState spaceVendorsUiState)
            return;

        _fragment?.UpdateState(spaceVendorsUiState.AppraisedItems);
    }
}
