using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private static readonly EntityTimerId PingCooldownTimer = new("ping-cooldown");
    private static readonly EntityTimerId MapDequeueTimer = new("map-dequeue");

    [ViewVariables]
    private ShuttleConsoleWindow? _window;

    public ShuttleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ShuttleConsoleWindow>();

        _window.RequestFTL += OnFTLRequest;
        _window.RequestBeaconFTL += OnFTLBeaconRequest;
        _window.DockRequest += OnDockRequest;
        _window.UndockRequest += OnUndockRequest;
        _window.PingCooldownStarted += delay => SetTimer(PingCooldownTimer, delay);
        _window.MapDequeueRequested += delay => SetTimer(MapDequeueTimer, delay);
    }

    protected override void OnTimer(EntityTimerEvent timer)
    {
        if (timer.Id == PingCooldownTimer)
            _window?.OnPingCooldownTimer();
        else if (timer.Id == MapDequeueTimer)
            _window?.OnMapDequeueTimer();
    }

    private void OnUndockRequest(NetEntity entity)
    {
        SendMessage(new UndockRequestMessage()
        {
            DockEntity = entity,
        });
    }

    private void OnDockRequest(NetEntity entity, NetEntity target)
    {
        SendMessage(new DockRequestMessage()
        {
            DockEntity = entity,
            TargetDockEntity = target,
        });
    }

    private void OnFTLBeaconRequest(NetEntity ent, Angle angle)
    {
        SendMessage(new ShuttleConsoleFTLBeaconMessage()
        {
            Beacon = ent,
            Angle = angle,
        });
    }

    private void OnFTLRequest(MapCoordinates obj, Angle angle)
    {
        SendMessage(new ShuttleConsoleFTLPositionMessage()
        {
            Coordinates = obj,
            Angle = angle,
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ShuttleBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(Owner, cState);
    }
}
