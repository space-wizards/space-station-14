using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class CriminalRecordsCatridgeUi : UIFragment
{
    private CriminalRecordsCatridgeUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new CriminalRecordsCatridgeUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not CriminalRecordsCartridgeUiState criminalRecordsCartridgeUiState)
            return;

        _fragment?.UpdateState(criminalRecordsCartridgeUiState.Records);
    }
}
