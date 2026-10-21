using Content.Server.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Payloads;
using Content.Shared.DeviceLinking.Systems;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

public sealed partial class SignalControlledValveSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _signal = default!;
    [Dependency] private GasValveSystem _valve = default!;
    [Dependency] private EntityQuery<GasValveComponent> _valveQuery;

    private void OnInit(EntityUid uid, SignalControlledValveComponent comp, ComponentInit args)
    {
        _signal.EnsureSinkPorts(uid, comp.OpenPort, comp.ClosePort, comp.TogglePort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalControlledValveComponent> ent, ref SignalReceivedEvent args)
    {
        if (!_valveQuery.TryComp(ent.Owner, out var valve))
            return;

        if (args.Port == ent.Comp.OpenPort)
            _valve.Set(ent.Owner, valve, true);
        else if (args.Port == ent.Comp.ClosePort)
            _valve.Set(ent.Owner, valve, false);
        else if (args.Port == ent.Comp.TogglePort)
            _valve.Toggle(ent.Owner, valve);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<SignalControlledValveComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        if (!_valveQuery.TryComp(ent.Owner, out var valve))
            return;
 
        var state = args.Data.State;
 
        if (args.Port == ent.Comp.OpenPort)
        {
            if (state != SignalState.Low)
                _valve.Set(ent.Owner, valve, true);
        }
        else if (args.Port == ent.Comp.ClosePort)
        {
            if (state != SignalState.Low)
                _valve.Set(ent.Owner, valve, false);
        }
        else if (args.Port == ent.Comp.TogglePort)
        {
            if (state != SignalState.Low)
                _valve.Toggle(ent.Owner, valve);
        }
    }
}
