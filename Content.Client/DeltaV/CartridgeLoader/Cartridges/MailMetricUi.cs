using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Client.DeltaV.CartridgeLoader.Cartridges;

public sealed partial class MailMetricUi : UIFragment
{
    private MailMetricUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MailMetricUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is MailMetricUiState cast)
        {
            _fragment?.UpdateState(cast);
        }
    }
}
