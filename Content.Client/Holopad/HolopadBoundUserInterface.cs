using Content.Shared.Holopad;
using Robust.Client.UserInterface;

namespace Content.Client.Holopad;

public sealed class HolopadBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolopadWindow? _menu;

    public HolopadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _menu = this.CreateWindow<HolopadWindow>();

        _menu.SendHolopadStartNewCallMessageAction += SendHolopadStartNewCallMessage;
        _menu.SendHolopadAnswerCallMessageAction += SendHolopadAnswerCallMessage;
        _menu.SendHolopadHangUpOnCallMessageAction += SendHolopadHangUpOnCallMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (HolopadBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

        _menu?.UpdateUIState(castState.State, castState.Holopads);
    }

    public void SendHolopadStartNewCallMessage(NetEntity receiver)
    {
        SendMessage(new HolopadStartNewCallMessage(receiver));
    }

    public void SendHolopadAnswerCallMessage()
    {
        SendMessage(new HolopadAnswerCallMessage());
    }

    public void SendHolopadHangUpOnCallMessage()
    {
        SendMessage(new HolopadHangUpOnCallMessage());
    }
}
