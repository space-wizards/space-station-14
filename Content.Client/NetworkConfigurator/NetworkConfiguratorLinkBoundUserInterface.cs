using System.Linq;
using Content.Shared.DeviceConfigurator;
using Content.Shared.DeviceConfigurator.Components;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Client.UserInterface;

namespace Content.Client.NetworkConfigurator;

public sealed partial class NetworkConfiguratorLinkBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NetworkConfiguratorLinkMenu? _linkMenu;

    [Dependency] private DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;
    [Dependency] private EntityQuery<DeviceLinkSinkComponent> _deviceLinkSinkQuery = default!;
    [Dependency] private EntityQuery<DeviceLinkSourceComponent> _deviceLinkSourceQuery = default!;
    [Dependency] private EntityQuery<NetworkConfiguratorComponent> _configQuery = default!;

    public NetworkConfiguratorLinkBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _linkMenu = this.CreateWindow<NetworkConfiguratorLinkMenu>();
        _linkMenu.OnLinkDefaults += args => SendPredictedMessage(new NetworkConfiguratorLinkSaveMessage(args));
        _linkMenu.OnToggleLink += link => SendPredictedMessage(new NetworkConfiguratorLinkToggleMessage(link));
        _linkMenu.OnClearLinks += () => SendPredictedMessage(new NetworkConfiguratorLinkClearMessage());
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (!_configQuery.TryComp(Owner, out var configComp)
            || configComp.DeviceLinkTarget == null
            || configComp.ActiveDeviceLink == null)
            return;

        // Target is the source, Active is the sink
        var source = configComp.DeviceLinkTarget.Value;
        var sink = configComp.ActiveDeviceLink.Value;

        // Active is the source, Target is the sink
        if (_deviceLinkSourceQuery.HasComp(configComp.ActiveDeviceLink)
            && _deviceLinkSinkQuery.HasComp(configComp.DeviceLinkTarget))
        {
            source = configComp.ActiveDeviceLink.Value;
            sink = configComp.DeviceLinkTarget.Value;
        }

        if (!_deviceLinkSourceQuery.TryComp(source, out var sourceComp)
            || !_deviceLinkSinkQuery.TryComp(sink, out var sinkComp))
            return;

        var sources = _deviceLinkSystem.GetSourcePorts((source, sourceComp));
        var sinks = _deviceLinkSystem.GetSinkPortIds((sink, sinkComp));
        var links = _deviceLinkSystem.GetLinks((source, sourceComp), sink);
        var defaults = _deviceLinkSystem.GetDefaults(sources);
        var sourceIds = sources.ToArray();

        var sourceAddress = string.Empty;
        var sinkAddress = string.Empty;
        var sourceAddressId = DeviceAddress.Invalid;
        var sinkAddressId = DeviceAddress.Invalid;
        if (_deviceNetworkQuery.TryComp(source, out var sourceDeviceComp))
        {
            sourceAddress = _deviceNetwork.GetAddress((source, sourceDeviceComp));
            sourceAddressId = sourceDeviceComp.Data.AddressId;
        }

        if (_deviceNetworkQuery.TryComp(sink, out var sinkDeviceComp))
        {
            sinkAddress = _deviceNetwork.GetAddress((sink, sinkDeviceComp));
            sinkAddressId = sinkDeviceComp.Data.AddressId;
        }

        _linkMenu?.UpdateState(sourceIds,
            sinks,
            links,
            sourceAddressId,
            sinkAddressId,
            sourceAddress,
            sinkAddress,
            defaults);
    }
}
