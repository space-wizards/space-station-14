using Content.Shared.Holopad;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using System.Numerics;

namespace Content.Client.Holopad;

public sealed class HolopadBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;

    [ViewVariables]
    private HolopadWindow? _window;

    public HolopadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HolopadWindow>();
        _window.Title = Loc.GetString("holopad-window-title", ("title", EntMan.GetComponent<MetaDataComponent>(Owner).EntityName));

        if (this.UiKey is not HolopadUiKey)
        {
            Close();
            return;
        }

        var uiKey = (HolopadUiKey)this.UiKey;

        // AIs will see a different holopad interface to crew when interacting with them in the world
        if (uiKey == HolopadUiKey.InteractionWindow && EntMan.HasComponent<StationAiHeldComponent>(_playerManager.LocalEntity))
            uiKey = HolopadUiKey.InteractionWindowForAi;

        _window.SetState(Owner, uiKey);
        _window.UpdateState(new Dictionary<NetEntity, string>());

        // Set message actions
        _window.SendHolopadStartNewCallMessageAction += SendHolopadStartNewCallMessage;
        _window.SendHolopadAnswerCallMessageAction += SendHolopadAnswerCallMessage;
        _window.SendHolopadEndCallMessageAction += SendHolopadEndCallMessage;
        _window.SendHolopadStartBroadcastMessageAction += SendHolopadStartBroadcastMessage;
        _window.SendHolopadActivateProjectorMessageAction += SendHolopadActivateProjectorMessage;
        _window.SendHolopadRequestStationAiMessageAction += SendHolopadRequestStationAiMessage;

        // If this call is addressed to an AI, open the window in the bottom right hand corner of the screen
        if (uiKey == HolopadUiKey.AiRequestWindow)
            _window.OpenCenteredAt(new Vector2(1f, 1f));

        // Otherwise offset to the left so the holopad can still be seen
        else
            _window.OpenCenteredAt(new Vector2(0.3333f, 0.50f));
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
        SendMessage(new HolopadStationAiRequestMessage());
    }
}
