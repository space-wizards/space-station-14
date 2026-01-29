using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechGeneratorUi : UIFragment
{
    [Dependency] private readonly IEntityManager _entMan = default!;

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
        _fragment.OnEject += () =>
        {
            userInterface.SendMessage(new MechGeneratorEjectFuelMessage(_entMan.GetNetEntity(fragmentOwner.Value)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGeneratorUiState genState)
            return;

        _fragment?.UpdateContents(genState);
    }
}
