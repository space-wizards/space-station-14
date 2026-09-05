using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

/// <summary>
/// A UI fragment for the NetProbe PDA app.
/// </summary>
public sealed partial class NetProbeUi : UIFragment
{
    private NetProbeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = userInterface.CreateDisposableControl<NetProbeUiFragment>();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NetProbeUiState netProbeUiState)
            return;

        _fragment?.UpdateState(netProbeUiState.ProbedDevices);
    }
}
