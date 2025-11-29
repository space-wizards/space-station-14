using Robust.Shared.Random;
using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server.DeviceLinking.Systems;

public sealed class RandomGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RandomGateComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(EntityUid uid, RandomGateComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.InputPort)
            return;

        var output = _random.Prob(0.5f);
        if (output != comp.LastOutput)
        {
            comp.LastOutput = output;
            _deviceLink.SendSignal(uid, comp.OutputPort, output);
        }
    }
}
