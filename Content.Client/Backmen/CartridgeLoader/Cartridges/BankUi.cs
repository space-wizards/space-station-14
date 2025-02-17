// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.UserInterface.Fragments;
using Content.Shared.Backmen.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.Backmen.CartridgeLoader.Cartridges;

public sealed partial class BankUi : UIFragment
{
    public BankUiFragment? Fragment;

    public override Control GetUIFragmentRoot()
    {
        return Fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        Fragment = new BankUiFragment();
        Fragment.UpdateEntity(fragmentOwner);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BankUiState bankState)
            return;

        Fragment?.UpdateState(bankState);
    }
}
