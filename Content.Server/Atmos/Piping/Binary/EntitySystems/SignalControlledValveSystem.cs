using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems;

public sealed class SignalControlledValveSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signal = default!;
    [Dependency] private readonly GasValveSystem _valve = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalControlledValveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalControlledValveComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, SignalControlledValveComponent comp, ComponentInit args)
    {
        _signal.EnsureSinkPorts(uid, comp.OpenPort, comp.ClosePort, comp.TogglePort);
    }

    private void OnSignalReceived(EntityUid uid, SignalControlledValveComponent comp, ref SignalReceivedEvent args)
    {
        if (!TryComp<GasValveComponent>(uid, out var valve))
            return;

        if (args.Port == comp.OpenPort)
        {
            _valve.Set(uid, valve, true);
        }
        else if (args.Port == comp.ClosePort)
        {
            _valve.Set(uid, valve, false);
        }
        else if (args.Port == comp.TogglePort)
        {
            _valve.Toggle(uid, valve);
        }
    }
}
