// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Client.Cargo.UI;
using Content.Shared.Cargo.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class FundingAllocationConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private FundingAllocationMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<FundingAllocationMenu>();

        _menu.OnSavePressed += (dicts, primary, lockbox) =>
        {
            SendMessage(new SetFundingAllocationBuiMessage(dicts, primary, lockbox));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not FundingAllocationConsoleBuiState state)
            return;

        _menu?.Update(state);
    }
}
