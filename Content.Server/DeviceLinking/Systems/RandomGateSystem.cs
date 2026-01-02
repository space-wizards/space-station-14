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

    private void OnSignalReceived(Entity<RandomGateComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.InputPort)
            return;

        var output = _random.Prob(ent.Comp.SuccessProbability);
        if (output != ent.Comp.LastOutput)
        {
            ent.Comp.LastOutput = output;
            Dirty(ent);
            _deviceLink.SendSignal(ent.Owner, ent.Comp.OutputPort, output);
        }
    }
}
