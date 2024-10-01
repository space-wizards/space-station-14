using Content.Shared.Holopad;
using Robust.Client.UserInterface;

namespace Content.Client.Holopad;

public sealed class HolopadBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolopadWindow? _window;

    public HolopadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _window = this.CreateWindow<HolopadWindow>();

        _window.SendHolopadStartNewCallMessageAction += SendHolopadStartNewCallMessage;
        _window.SendHolopadAnswerCallMessageAction += SendHolopadAnswerCallMessage;
        _window.SendHolopadEndCallMessageAction += SendHolopadEndCallMessage;
        _window.SendHolopadStartBroadcastMessageAction += SendHolopadStartBroadcastMessage;
        _window.SendHolopadActivateProjectorMessageAction += SendHolopadActivateProjectorMessage;
        _window.SendHolopadRequestStationAiMessageAction += SendHolopadRequestStationAiMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (HolopadBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

        _window?.UpdateUIState(castState.State, castState.Holopads);
    }

    public void SendHolopadStartNewCallMessage(NetEntity receiver)
    {
        SendMessage(new HolopadStartNewCallMessage(receiver));
    }

    public void SendHolopadAnswerCallMessage()
    {
        SendMessage(new HolopadAnswerCallMessage());
    }

    public void SendHolopadEndCallMessage()
    {
        SendMessage(new HolopadEndCallMessage());
    }

    public void SendHolopadStartBroadcastMessage()
    {
        SendMessage(new HolopadStartBroadcastMessage());
    }

    public void SendHolopadActivateProjectorMessage()
    {
        SendMessage(new HolopadActivateProjectorMessage());
    }

    public void SendHolopadRequestStationAiMessage()
    {
        SendMessage(new HolopadRequestStationAiMessage());
    }
}
