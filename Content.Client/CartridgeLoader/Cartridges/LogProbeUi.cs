using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class LogProbeUi : UIFragment
{
    private LogProbeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new LogProbeUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not LogProbeUiState logProbeUiState)
            return;

        _fragment?.UpdateState(logProbeUiState); // DeltaV - just take the state
    }
}
