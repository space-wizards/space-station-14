using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Handles the control of output based on the input and enable ports.
/// </summary>
public sealed class MemoryCellSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MemoryCellComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MemoryCellComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var query = EntityQueryEnumerator<MemoryCellComponent, DeviceLinkSourceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            if (comp.InputState == SignalState.Momentary)
                comp.InputState = SignalState.Low;
            if (comp.EnableState == SignalState.Momentary)
                comp.EnableState = SignalState.Low;

            UpdateOutput((uid, comp, source));
        }
    }

    private void OnInit(Entity<MemoryCellComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort, comp.EnablePort);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);
    }

    private void OnSignalReceived(Entity<MemoryCellComponent> ent, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (args.Port == ent.Comp.InputPort)
            ent.Comp.InputState = state;
        else if (args.Port == ent.Comp.EnablePort)
            ent.Comp.EnableState = state;

        UpdateOutput(ent);
    }

    private void UpdateOutput(Entity<MemoryCellComponent, DeviceLinkSourceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        if (ent.Comp1.EnableState == SignalState.Low)
            return;

        var value = ent.Comp1.InputState != SignalState.Low;
        if (value == ent.Comp1.LastOutput)
            return;

        ent.Comp1.LastOutput = value;
        _deviceLink.SendSignal(ent, ent.Comp1.OutputPort, value, ent.Comp2);
    }
}
