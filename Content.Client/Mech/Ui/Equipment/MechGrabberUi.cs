﻿using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechGrabberUi : UIFragment
{
    [Dependency] private readonly IEntityManager _entMan = default!;

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
            userInterface.SendMessage(new MechGrabberEjectMessage(_entMan.GetNetEntity(fragmentOwner.Value),
                _entMan.GetNetEntity(e)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGrabberUiState grabberState)
            return;

        _fragment?.UpdateContents(grabberState);
    }
}
