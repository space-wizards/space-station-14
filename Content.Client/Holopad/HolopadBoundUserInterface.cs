using Content.Shared.Holopad;
using Content.Shared.Power;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.Holopad;

public sealed class HolopadBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolopadWindow? _menu;

    public HolopadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _menu = this.CreateWindow<HolopadWindow>();
        _menu.SendHolopadMessageAction += SendHolopadMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (HolopadBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

        _menu?.UpdateUIState(castState.Holopads);
    }

    public void SendHolopadMessage(NetEntity caller, NetEntity recipient)
    {
        SendMessage(new HolopadMessage(caller, recipient));
    }
}
