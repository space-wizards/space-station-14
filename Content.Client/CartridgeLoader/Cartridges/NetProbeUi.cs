using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class NetProbeUi : CartridgeUI
{
    private NetProbeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface)
    {
        _fragment = new NetProbeUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NetProbeUiState netProbeUiState)
            return;

        _fragment?.UpdateState(netProbeUiState.ProbedDevices);
    }
}
