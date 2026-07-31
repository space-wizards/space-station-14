using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class LogicGateSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    [Dependency] private EntityQuery<UseDelayComponent> _useDelayQuery = default!;

    private readonly int _gateCount = Enum.GetValues<LogicGate>().Length;

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<LogicGateComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // handle momentary pulses - high when received then low the next tick
            if (comp.StateA == SignalState.Momentary)
            {
                comp.StateA = SignalState.Low;
            }
            if (comp.StateB == SignalState.Momentary)
            {
                comp.StateB = SignalState.Low;
            }

            // output most likely changed so update it
            UpdateOutput((uid, comp));
        }
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<LogicGateComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.InputPortA, ent.Comp.InputPortB);
        _deviceLink.EnsureSourcePort(ent.Owner, ent.Comp.OutputPort);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<LogicGateComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("logic-gate-examine", ("gate", ent.Comp.Gate.ToString().ToUpper())));
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<LogicGateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.CycleQuality))
            return;

        // no sound spamming
        if (_useDelayQuery.TryComp(ent.Owner, out var useDelay)
            && !_useDelay.TryResetDelay((ent.Owner, useDelay), true))
            return;

        // cycle through possible gates
        var gate = (int) ent.Comp.Gate;
        gate = ++gate % _gateCount;
        ent.Comp.Gate = (LogicGate) gate;

        // since gate changed the output probably has too, update it
        UpdateOutput(ent);

        // notify the user
        _audio.PlayPvs(ent.Comp.CycleSound, ent.Owner);
        var msg = Loc.GetString("logic-gate-cycle", ("gate", ent.Comp.Gate.ToString().ToUpper()));
        _popup.PopupEntity(msg, ent.Owner, args.User);
        _appearance.SetData(ent.Owner, LogicGateVisuals.Gate, ent.Comp.Gate);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<LogicGateComponent> ent, ref SignalReceivedEvent args)
    {
        // default to momentary for compatibility with non-logic signals.
        // currently only door status and logic gates have logic signal state.

        // update the state for the correct port
        if (args.Port == ent.Comp.InputPortA)
        {
            ent.Comp.StateA = SignalState.Momentary;
            _appearance.SetData(ent.Owner, LogicGateVisuals.InputA, false); //If A == High => Sets input A sprite to True
        }
        else if (args.Port == ent.Comp.InputPortB)
        {
            ent.Comp.StateB = SignalState.Momentary;
            _appearance.SetData(ent.Owner, LogicGateVisuals.InputB, false); //If B == High => Sets input B sprite to True
        }

        UpdateOutput(ent);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<LogicGateComponent> ent, ref SignalReceivedEvent<LogicStatePayload> args)
    {
        var state = args.Data.State;

        // update the state for the correct port
        if (args.Port == ent.Comp.InputPortA)
        {
            ent.Comp.StateA = state;
            _appearance.SetData(ent.Owner, LogicGateVisuals.InputA, state == SignalState.High); //If A == High => Sets input A sprite to True
        }
        else if (args.Port == ent.Comp.InputPortB)
        {
            ent.Comp.StateB = state;
            _appearance.SetData(ent.Owner, LogicGateVisuals.InputB, state == SignalState.High); //If B == High => Sets input B sprite to True
        }

        UpdateOutput(ent);
    }

    /// <summary>
    /// Handle the logic for a logic gate, invoking the port if the output changed.
    /// </summary>
    private void UpdateOutput(Entity<LogicGateComponent> ent)
    {
        // get the new output value now that it's changed
        // momentary is treated as high for the current tick, after updating it will be reset to low
        var a = ent.Comp.StateA != SignalState.Low;
        var b = ent.Comp.StateB != SignalState.Low;
        var output = false;
        switch (ent.Comp.Gate)
        {
            case LogicGate.Or:
                output = a || b;
                break;
            case LogicGate.And:
                output = a && b;
                break;
            case LogicGate.Xor:
                output = a != b;
                break;
            case LogicGate.Nor:
                output = !(a || b);
                break;
            case LogicGate.Nand:
                output = !(a && b);
                break;
            case LogicGate.Xnor:
                output = a == b;
                break;
        }

        _appearance.SetData(ent.Owner, LogicGateVisuals.Output, output);

        // only send a payload if it actually changed
        if (output != ent.Comp.LastOutput)
        {
            ent.Comp.LastOutput = output;

            _deviceLink.SendSignal(ent.Owner, ent.Comp.OutputPort, output);
        }
    }
}
