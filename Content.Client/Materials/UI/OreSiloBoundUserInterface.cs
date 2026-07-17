using Content.Shared.Materials.OreSilo;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Materials.UI;

[UsedImplicitly]
public sealed class OreSiloBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private OreSiloMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<OreSiloMenu>();
        _menu.SetEntity(Owner);

        _menu.OnClientEntryPressed += netEnt =>
        {
            SendPredictedMessage(new ToggleOreSiloClientMessage(netEnt));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not OreSiloBuiState msg)
            return;
        _menu?.Update(msg);
    }
}
