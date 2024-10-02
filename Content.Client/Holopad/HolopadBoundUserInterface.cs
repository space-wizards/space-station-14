using Content.Shared.Holopad;
using Robust.Client.UserInterface;
using System.Numerics;

namespace Content.Client.Holopad;

public sealed class HolopadBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolopadWindow? _window;

    public HolopadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HolopadWindow>();

        if (this.UiKey is not HolopadUiKey)
        {
            Close();
            return;
        }

        var uiKey = (HolopadUiKey)this.UiKey;

        _window.SetState(Owner, uiKey);
        _window.UpdateState(new Dictionary<NetEntity, string>());

        _window.SendHolopadStartNewCallMessageAction += SendHolopadStartNewCallMessage;
        _window.SendHolopadAnswerCallMessageAction += SendHolopadAnswerCallMessage;
        _window.SendHolopadEndCallMessageAction += SendHolopadEndCallMessage;
        _window.SendHolopadStartBroadcastMessageAction += SendHolopadStartBroadcastMessage;
        _window.SendHolopadActivateProjectorMessageAction += SendHolopadActivateProjectorMessage;
        _window.SendHolopadRequestStationAiMessageAction += SendHolopadRequestStationAiMessage;

        // If this is a request for an AI, open the menu 
        // in the bottom right hand corner of the screen
        if (uiKey == HolopadUiKey.AiRequestWindow)
            _window.OpenCenteredAt(new Vector2(1f, 1f));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (HolopadBoundInterfaceState)state;

        EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);

        _window?.UpdateState(castState.Holopads);
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
