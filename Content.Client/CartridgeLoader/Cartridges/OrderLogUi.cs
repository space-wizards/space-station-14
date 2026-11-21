using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class OrderLogUi : UIFragment
{
    private OrderLogUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new OrderLogUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not OrderLogUiState OrderLogState)
            return;

        _fragment?.UpdateState(OrderLogState.Orders);
    }
}
