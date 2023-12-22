using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Server.NodeContainer;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.Generator;
using Content.Shared.Timing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed class PowerSensorSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PowerNetSystem _powerNet = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<NodeContainerComponent> _nodeQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _nodeQuery = GetEntityQuery<NodeContainerComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PowerSensorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PowerSensorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PowerSensorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<PowerSensorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var now = _timing.CurTime;
            if (comp.NextCheck > now)
                continue;

            comp.NextCheck = now + comp.CheckDelay;
            UpdateOutputs(uid, comp);
        }
    }

    private void OnInit(EntityUid uid, PowerSensorComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.ChargingPort, comp.DischargingPort);
    }

    private void OnExamined(EntityUid uid, PowerSensorComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("power-sensor-examine", ("output", comp.Output)));
    }

    private void OnInteractUsing(EntityUid uid, PowerSensorComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, comp.SwitchQuality))
            return;

        // no sound spamming
        if (TryComp<UseDelayComponent>(uid, out var useDelay) && _useDelay.ActiveDelay(uid, useDelay))
            return;

        // switch between input and output mode.
        comp.Output = !comp.Output;

        // since the battery to be checked changed the output probably has too, update it
        UpdateOutputs(uid, comp);

        // notify the user
        _audio.PlayPvs(comp.SwitchSound, uid);
        var msg = Loc.GetString("power-sensor-switch", ("output", comp.Output));
        _popup.PopupEntity(msg, uid, args.User);

        _useDelay.BeginDelay(uid, useDelay);
    }

    private void UpdateOutputs(EntityUid uid, PowerSensorComponent comp)
    {
        // get power stats on the power network that's been switched to
        var powerSwitchable = Comp<PowerSwitchableComponent>(uid);
        var cable = powerSwitchable.Cables[powerSwitchable.ActiveIndex];
        var nodeContainer = Comp<NodeContainerComponent>(uid);
        var deviceNode = (CableDeviceNode) nodeContainer.Nodes[cable.Node];

        var charge = 0f;
        var chargingState = false;
        var dischargingState = false;

        // update state based on the power stats retrieved from the selected power network
        var xform = _xformQuery.GetComponent(uid);
        _mapManager.TryGetGrid(xform.GridUid, out var grid);
        var cables = deviceNode.GetReachableNodes(xform, _nodeQuery, _xformQuery, grid, EntityManager);
        foreach (var node in cables)
        {
            if (node.NodeGroup == null)
                continue;

            var group = (IBasePowerNet) node.NodeGroup;
            var stats = _powerNet.GetNetworkStatistics(group.NetworkNode);
            charge = comp.Output ? stats.OutStorageCurrent : stats.InStorageCurrent;
            chargingState = charge > comp.LastCharge;
            dischargingState = charge < comp.LastCharge;
            break;
        }

        comp.LastCharge = charge;

        // send new signals if changed
        if (comp.ChargingState != chargingState)
        {
            comp.ChargingState = chargingState;
            _deviceLink.SendSignal(uid, comp.ChargingPort, chargingState);
        }

        if (comp.DischargingState != dischargingState)
        {
            comp.DischargingState = dischargingState;
            _deviceLink.SendSignal(uid, comp.DischargingPort, dischargingState);
        }
    }
}
