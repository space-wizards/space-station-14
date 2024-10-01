using Content.Shared.Holopad;
using Robust.Client.UserInterface;

namespace Content.Client.Holopad;

public sealed class HolopadIncomingCallBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolopadIncomingCallWindow? _window;

    public HolopadIncomingCallBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _window = this.CreateWindow<HolopadIncomingCallWindow>();

        _window.SendHolopadAnswerCallMessageAction += SendHolopadAnswerCallMessage;
        _window.SendHolopadEndCallMessageAction += SendHolopadEndCallMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (HolopadBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

        _window?.UpdateUIState(castState.State, castState.Holopads);
    }

    public void SendHolopadAnswerCallMessage()
    {
        SendMessage(new HolopadAnswerCallMessage());
    }

    public void SendHolopadEndCallMessage()
    {
        SendMessage(new HolopadEndCallMessage());
    }
}
