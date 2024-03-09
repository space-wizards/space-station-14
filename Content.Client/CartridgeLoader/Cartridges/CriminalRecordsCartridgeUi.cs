using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class CriminalRecordsCartridgeUi : UIFragment
{
    private CriminalRecordsCartridgeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CriminalRecordsCartridgeUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CriminalRecordsCartridgeUiState uiState)
            return;

        _fragment?.UpdateState(uiState);
    }
}
