using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed class MechGrabberUi : UIFragment
{
    private MechGrabberUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        _fragment = new MechGrabberUiFragment();
        _fragment.OnEjectAction += e =>
        {
            userInterface.SendMessage(new MechGrabberEjectMessage(fragmentOwner.Value, e));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGrabberUiState grabberState)
            return;

        _fragment?.UpdateContents(grabberState);
    }
}
