using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechGeneratorUi : UIFragment
{
    private MechGeneratorUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        _fragment = new MechGeneratorUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGeneratorUiState genState)
            return;

        _fragment?.UpdateContents(genState);
    }
}
