using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class CrewManifestUi : UIFragment
{
    private CrewManifestUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CrewManifestUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CrewManifestUiState crewManifestState)
            return;

        _fragment?.UpdateState(crewManifestState.StationName, crewManifestState.Entries);
    }
}
