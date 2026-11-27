using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

/// <summary>
/// A UI fragment for hydraulic clamp mech controls.
/// </summary>
public sealed partial class MechGrabberUi : UIFragment
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

        _fragment = userInterface.CreateDisposableControl<MechGrabberUiFragment>();

        _fragment.OnEjectAction += e =>
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            userInterface.SendMessage(new MechGrabberEjectMessage(entManager.GetNetEntity(fragmentOwner.Value), entManager.GetNetEntity(e)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGrabberUiState grabberState)
            return;

        _fragment?.UpdateContents(grabberState);
    }
}
