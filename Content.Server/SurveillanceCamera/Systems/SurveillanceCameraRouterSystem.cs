using Content.Server.DeviceNetwork.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.SurveillanceCamera;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.SurveillanceCamera;

public sealed partial class SurveillanceCameraRouterSystem : DevicePayloadSystem<SurveillanceCameraRouterComponent>
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private EntityQuery<SurveillanceCameraRouterComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, SurveillanceCameraSetupSetNetwork>(OnSetNetwork);
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbs);
        SubscribeLocalEvent<SurveillanceCameraRouterComponent, PowerChangedEvent>(OnPowerChanged);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<SurveillanceCameraSubnetConnectPayload>(OnSubnetConnect);
        SubscribePayload<SurveillanceCameraSubnetDisconnectPayload>(OnSubnetDisconnect);
        SubscribePayload<SurveillanceCameraPingSubnetPayload>(OnSubnetPing);
    }

    private void OnMapInit(Entity<SurveillanceCameraRouterComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out DeviceNetworkRouterComponent? router))
            router.TransmitFrequency = ent.Comp.SubnetFrequency;

        if (ent.Comp.SubnetFrequencyId == null
            || !ProtoMan.TryIndex(ent.Comp.SubnetFrequencyId, out var subnetFrequency))
            return;

        ent.Comp.SubnetFrequency = subnetFrequency.Frequency;
        ent.Comp.Active = true;

        if (string.IsNullOrEmpty(ent.Comp.SubnetName) && subnetFrequency.Name != null)
            ent.Comp.SubnetName = Loc.GetString(subnetFrequency.Name);
    }

    private void OnSubnetConnect(
        Entity<SurveillanceCameraRouterComponent> ent,
        ref SurveillanceCameraSubnetConnectPayload payload,
        ref DeviceNetworkPacketData args)
    {
        AddMonitorToRoute(ent.AsNullable(), args.SenderAddress);
        PingSubnet(ent.AsNullable());
    }

    private void OnSubnetDisconnect(
        Entity<SurveillanceCameraRouterComponent> ent,
        ref SurveillanceCameraSubnetDisconnectPayload payload,
        ref DeviceNetworkPacketData args)
    {
        RemoveMonitorFromRoute(ent.AsNullable(), args.SenderAddress);
    }

    private void OnSubnetPing(
        Entity<SurveillanceCameraRouterComponent> ent,
        ref SurveillanceCameraPingSubnetPayload payload,
        ref DeviceNetworkPacketData args)
    {
        var response = new SurveillanceCameraSubnetDataPayload
        {
            Subnet = ent.Comp.SubnetName,
        };
        _deviceNetworkSystem.QueuePacket(ent.Owner, args.SenderAddress, response);
    }

    private void OnPowerChanged(Entity<SurveillanceCameraRouterComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.MonitorRoutes.Clear();
        ent.Comp.Active = args.Powered;
    }

    private void AddVerbs(Entity<SurveillanceCameraRouterComponent> ent, ref GetVerbsEvent<AlternativeVerb> verbs)
    {
        if (!_actionBlocker.CanInteract(verbs.User, ent.Owner) || !_actionBlocker.CanComplexInteract(verbs.User))
        {
            return;
        }

        if (ent.Comp.SubnetFrequencyId != null)
        {
            return;
        }

        AlternativeVerb verb = new();
        var user = verbs.User;
        verb.Text = Loc.GetString("surveillance-camera-setup");
        verb.Act = () => OpenSetupInterface(ent.AsNullable(), user);
        verbs.Verbs.Add(verb);
    }

    private void OnSetNetwork(Entity<SurveillanceCameraRouterComponent> ent, ref SurveillanceCameraSetupSetNetwork args)
    {
        if (args.UiKey is not SurveillanceCameraSetupUiKey key
            || key != SurveillanceCameraSetupUiKey.Router)
        {
            return;
        }
        if (args.Network < 0 || args.Network >= ent.Comp.AvailableNetworks.Count)
        {
            return;
        }

        if (!ProtoMan.Resolve(ent.Comp.AvailableNetworks[args.Network], out var frequency))
        {
            return;
        }

        ent.Comp.SubnetFrequencyId = ent.Comp.AvailableNetworks[args.Network];
        ent.Comp.SubnetFrequency = frequency.Frequency;
        ent.Comp.Active = true;
        UpdateSetupInterface(ent.AsNullable());
    }

    private void OpenSetupInterface(Entity<SurveillanceCameraRouterComponent?> ent, EntityUid player)
    {
        if (!_query.Resolve(ref ent))
            return;

        if (!_userInterface.TryOpenUi(ent.Owner, SurveillanceCameraSetupUiKey.Router, player))
            return;

        UpdateSetupInterface(ent.AsNullable());
    }

    private void UpdateSetupInterface(Entity<SurveillanceCameraRouterComponent?, DeviceNetworkComponent?> ent)
    {
        if (!_query.Resolve(ent.Owner, ref ent.Comp1) || !Resolve(ent.Owner, ref ent.Comp2))
            return;

        if (ent.Comp1.AvailableNetworks.Count == 0 || ent.Comp1.SubnetFrequencyId != null)
        {
            _userInterface.CloseUi(ent.Owner, SurveillanceCameraSetupUiKey.Router);
            return;
        }

        var state = new SurveillanceCameraSetupBoundUiState(ent.Comp1.SubnetName,
            ent.Comp2.ReceiveFrequency ?? 0,
            ent.Comp1.AvailableNetworks,
            true,
            ent.Comp1.SubnetFrequencyId != null);
        _userInterface.SetUiState(ent.Owner, SurveillanceCameraSetupUiKey.Router, state);
    }

    // Adds a monitor to the set of routes.
    private void AddMonitorToRoute(Entity<SurveillanceCameraRouterComponent?> ent, string address)
    {
        if (!_query.Resolve(ref ent) || ent.Comp == null)
            return;

        ent.Comp.MonitorRoutes.Add(address);
    }

    private void RemoveMonitorFromRoute(Entity<SurveillanceCameraRouterComponent?> ent, string address)
    {
        if (!_query.Resolve(ref ent) || ent.Comp == null)
            return;

        ent.Comp.MonitorRoutes.Remove(address);
    }

    // Pings a subnet to get all camera information.
    private void PingSubnet(Entity<SurveillanceCameraRouterComponent?> ent)
    {
        if (!_query.Resolve(ref ent) || ent.Comp == null)
            return;

        var payload = new SurveillanceCameraPingPayload
        {
            Subnet = ent.Comp.SubnetName,
        };
        _deviceNetworkSystem.QueuePacket(ent.Owner, null, payload, ent.Comp.SubnetFrequency);
    }
}
