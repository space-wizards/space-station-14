using Content.Client.Cargo.UI;
using Content.Shared.Cargo.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Cargo.BUI;

[UsedImplicitly]
public sealed class FundingAllocationConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FundingAllocationMenu? _menu;

    public FundingAllocationConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<FundingAllocationMenu>();

        _menu.OnSavePressed += d =>
        {
            SendMessage(new SetFundingAllocationBuiMessage(d));
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
