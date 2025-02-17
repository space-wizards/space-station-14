using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using static Content.Shared.Shuttles.Systems.SharedShuttleConsoleSystem;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class ShuttleConsoleBoundUserInterface : BoundUserInterface
{
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
        _window.ThrustersRestartRequest += OnThrustersRestartRequest;

        // DS14-start
        var navScreen = FindChildByType<NavScreen>(_window);

        if (navScreen != null)
        {
            navScreen.OnSignalButtonPressed += () =>
            {
                SendMessage(new ShuttleConsoleSignalButtonPressedMessage());
            };
        }
        // DS14-end
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

    private void OnThrustersRestartRequest(NetEntity ent, float gyroscopeThrust, float thrusterThrust)
    {
        SendMessage(new ThrustersRestartMessage()
        {
            ShuttleEntity = ent,
            GyroscopeThrust = gyroscopeThrust,
            ThrustersThrust = thrusterThrust,
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

    // DS14-start
    private T? FindChildByType<T>(Control parent) where T : class
    {
        foreach (var child in parent.Children)
        {
            if (child is T match)
                return match;

            if (child is Control container)
            {
                var result = FindChildByType<T>(container);
                if (result != null)
                    return result;
            }
        }

        return null;
    }
    // DS14-end
}
