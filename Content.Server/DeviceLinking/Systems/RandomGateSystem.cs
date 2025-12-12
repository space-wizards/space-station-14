using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Robust.Shared.Random;

namespace Content.Server.DeviceLinking.Systems;

public sealed class RandomGateSystem : SharedRandomGateSystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomGateComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(EntityUid uid, RandomGateComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port != component.InputPort)
            return;

        var output = _random.Prob(component.SuccessProbability);
        if (output != component.LastOutput)
        {
            component.LastOutput = output;
            _deviceLink.SendSignal(uid, component.OutputPort, output);
        }
    }
}
