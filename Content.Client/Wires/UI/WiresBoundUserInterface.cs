using Content.Shared.Wires;
using Robust.Client.UserInterface;

namespace Content.Client.Wires.UI;

/// <summary>
/// A BUI for maintenance panel wire interaction. Wraps a <see cref="WiresMenu"/>.
/// </summary>
/// <remarks>
/// Sends messages to the server when wires are cut/pulsed/mended.
/// </remarks>
/// <seealso cref="WiresPanelComponent"/>
public sealed class WiresBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private WiresMenu? _menu;

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
