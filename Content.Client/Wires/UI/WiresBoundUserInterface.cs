using Content.Shared.Wires;
using Robust.Client.UserInterface;

namespace Content.Client.Wires.UI;

/// <summary>
/// BUI for wire panels in doors, machines, etc.
/// Sends messages to the server when wires are cut/pulsed/mended.
/// </summary>
public sealed class WiresBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private WiresMenu? _menu;

    public WiresBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<WiresMenu>();
        _menu.OnAction += PerformAction;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _menu?.Populate((WiresBoundUserInterfaceState)state);
    }

    public void PerformAction(int id, WiresAction action)
    {
        SendMessage(new WiresActionMessage(id, action));
    }
}

