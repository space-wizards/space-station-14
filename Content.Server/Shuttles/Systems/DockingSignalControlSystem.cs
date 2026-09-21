using Content.Server.DeviceLinking.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Shuttles.Systems;

public sealed partial class DockingSignalControlSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private DockingSystem _dockingSystem = default!;

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<DockingSignalControlComponent> ent,
        ref ComponentInit args)
    {
        _deviceLinkSystem.EnsureSourcePorts(ent, ent.Comp.DockStatusSignalPort);
        _deviceLinkSystem.EnsureSinkPorts(ent, ent.Comp.DockTogglePort);
    }

    [SubscribeLocalEvent]
    private void OnDocked(Entity<DockingSignalControlComponent> ent, ref DockEvent args)
    {
        _deviceLinkSystem.SendSignal(ent, ent.Comp.DockStatusSignalPort, signal: true);
    }

    [SubscribeLocalEvent]
    private void OnUndocked(Entity<DockingSignalControlComponent> ent, ref UndockEvent args)
    {
        _deviceLinkSystem.SendSignal(ent, ent.Comp.DockStatusSignalPort, signal: false);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<DockingSignalControlComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.DockTogglePort)
            return;

        if (!TryComp<DockingComponent>(ent, out var dock))
            return;

        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        var shouldDock = state == SignalState.High || state == SignalState.Momentary && !dock.Docked;

        if (shouldDock)
        {
            var query = AllEntityQuery<DockingComponent>();
            while (query.MoveNext(out var dockingUid, out var docking))
            {
                if (!_dockingSystem.CanDock((ent, dock), (dockingUid, docking)))
                    continue;

                _dockingSystem.Dock((ent, dock), (dockingUid, docking));
                break;
            }
        }
        else
        {
            if (!_dockingSystem.CanUndock((ent, dock)))
                return;

            _dockingSystem.Undock((ent, dock));
        }
    }
}
