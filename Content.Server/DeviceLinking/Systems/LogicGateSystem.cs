using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;

namespace Content.Server.DeviceLinking.Systems;

public sealed class LogicGateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private readonly int GateCount = Enum.GetValues(typeof(LogicGate)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogicGateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LogicGateComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LogicGateComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LogicGateComponent, SignalReceivedEvent>(OnSignalReceived);
    }

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
                comp.StateB = SignalState.High;
            }

            // output most likely changed so update it
            UpdateOutput(uid, comp);
        }
    }

    private void OnInit(EntityUid uid, LogicGateComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPortA, comp.InputPortB);
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);
    }

    private void OnExamined(EntityUid uid, LogicGateComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("logic-gate-examine", ("gate", comp.Gate.ToString().ToUpper())));
    }

    private void OnInteractUsing(EntityUid uid, LogicGateComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, comp.CycleQuality))
            return;

        // no sound spamming
        if (TryComp<UseDelayComponent>(uid, out var useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        // cycle through possible gates
        var gate = (int) comp.Gate;
        gate = ++gate % GateCount;
        comp.Gate = (LogicGate) gate;

        // since gate changed the output probably has too, update it
        UpdateOutput(uid, comp);

        // notify the user
        _audio.PlayPvs(comp.CycleSound, uid);
        var msg = Loc.GetString("logic-gate-cycle", ("gate", comp.Gate.ToString().ToUpper()));
        _popup.PopupEntity(msg, uid, args.User);
        _appearance.SetData(uid, LogicGateVisuals.Gate, comp.Gate);
    }

    private void OnSignalReceived(EntityUid uid, LogicGateComponent comp, ref SignalReceivedEvent args)
    {
        // default to momentary for compatibility with non-logic signals.
        // currently only door status and logic gates have logic signal state.
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        // update the state for the correct port
        if (args.Port == comp.InputPortA)
        {
            comp.StateA = state;
        }
        else if (args.Port == comp.InputPortB)
        {
            comp.StateB = state;
        }

        UpdateOutput(uid, comp);
    }

    /// <summary>
    /// Handle the logic for a logic gate, invoking the port if the output changed.
    /// </summary>
    private void UpdateOutput(EntityUid uid, LogicGateComponent comp)
    {
        // get the new output value now that it's changed
        // momentary is treated as high for the current tick, after updating it will be reset to low
        var a = comp.StateA != SignalState.Low;
        var b = comp.StateB != SignalState.Low;
        var output = false;
        switch (comp.Gate)
        {
            case LogicGate.Or:
                output = a || b;
                UpdateState(uid,comp,a,b,output);
                break;
            case LogicGate.And:
                output = a && b;
                UpdateState(uid,comp,a,b,output);
                break;
            case LogicGate.Xor:
                output = a != b;
                UpdateState(uid,comp,a,b,output);
                break;
            case LogicGate.Nor:
                output = !(a || b);
                UpdateState(uid,comp,a,b,output);
                break;
            case LogicGate.Nand:
                output = !(a && b);
                UpdateState(uid,comp,a,b,output);
                break;
            case LogicGate.Xnor:
                output = a == b;
                UpdateState(uid,comp,a,b,output);
                break;
        }

        // only send a payload if it actually changed
        if (output != comp.LastOutput)
        {
            comp.LastOutput = output;

            _deviceLink.SendSignal(uid, comp.OutputPort, output);
        }
    }

    private void UpdateState(EntityUid uid, LogicGateComponent comp, bool a, bool b, bool output)
    {
        //im sorry for what im about to do

        if (a && b && output){
            comp.State = LogicGateState.LogicABO;
        _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (a && b && !output)
        {
            comp.State = LogicGateState.LogicAB;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (!a && b && output)
        {
            comp.State = LogicGateState.LogicBO;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (a && !b && output)
        {
            comp.State = LogicGateState.LogicAO;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (!a && !b && output)
        {
            comp.State = LogicGateState.LogicO;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (!a && !b && !output)
        {
            comp.State = LogicGateState.Logic;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (a && !b && !output)
        {
            comp.State = LogicGateState.LogicA;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }
        else if (!a && b && !output)
        {
            comp.State = LogicGateState.LogicB;
            _appearance.SetData(uid, LogicGateVisuals.State, comp.State);
        }

    }
}
