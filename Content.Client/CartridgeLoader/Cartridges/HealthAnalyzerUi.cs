using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class HealthAnalyzerUi : UIFragment
{
    private HealthAnalyzerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new HealthAnalyzerUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not HealthAnalyzerUiState healthAnalyzerUiState)
            return;

        _fragment?.UpdateState(healthAnalyzerUiState);

    }
}
