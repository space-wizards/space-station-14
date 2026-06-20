using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;
using Content.Server.Buffers;
using Content.Server.GameTicking.Events;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.GameTicking;
using Robust.Server.GameStates;

namespace Content.Server.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other while adhering to restrictions like range or being connected to the same powernet.
/// </summary>
[UsedImplicitly]
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();
        ArrayPool = new ServerRobustArrayPool<Device>();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<DeviceNetworkManagerComponent, MapInitEvent>(OnManagerInit);
        SubscribeLocalEvent<DeviceNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        EnsureManager();
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        ClearManager();
    }

    private void OnManagerInit(Entity<DeviceNetworkManagerComponent> ent, ref MapInitEvent args)
    {
        _pvsOverride.AddGlobalOverride(ent);
    }

    private void ClearManager()
    {
        if (TryGetManager(out var found))
            Del(found);
    }

    /// <summary>
    /// Removes the <see cref="DeviceNetworkManagerComponent"/> if it no longer has any entities in its networks.
    /// </summary>
    private void CheckClearManager()
    {
        if (!TryGetManager(out var found))
            return;

        foreach (var network in found.Value.Comp.Networks.Values)
        {
            if (network.Devices.Count != 0)
                return;
        }

        Del(found);
    }

    /// <summary>
    /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
    /// </summary>
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        var component = ent.Comp;
        foreach (var list in component.DeviceLists)
        {
            if (Deleted(list))
                return;

            _deviceLists.OnDeviceShutdown(list, ent);
        }

        foreach (var list in component.Configurators)
        {
            if (Deleted(list))
                return;

            _configurator.OnDeviceShutdown(list, ent);
        }

        if (TryGetNetwork(component.DeviceNetId, out var network))
            network.Remove(ent);

        CheckClearManager();
    }
}
