using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NetProbeCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
    }

    private void AfterInteract(EntityUid uid, NetProbeCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
            return;

        var target = args.InteractEvent.Target.Value;
        DeviceNetworkComponent? networkComponent = default;

        if (!Resolve(target, ref networkComponent, false))
            return;

        foreach (var probedDevice in component.ProbedDevices)
        {
            if (probedDevice.Address == networkComponent.Address)
                return;
        }

        //Limit the amount of saved probe results to 9
        //This is hardcoded because the UI doesn't support a dynamic number of results
        if (component.ProbedDevices.Count >= 9)
            component.ProbedDevices.RemoveAt(0);

        var device = new ProbedNetworkDevice(
            Name(target),
            networkComponent.Address,
            networkComponent.ReceiveFrequency?.FrequencyToString() ?? string.Empty,
            networkComponent.DeviceNetId.DeviceNetIdToString()
        );

        component.ProbedDevices.Add(device);
        UpdateUiState(uid, args.Loader, component);
    }

    private void OnUiReady(EntityUid uid, NetProbeCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NetProbeCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new NetProbeUiState(component.ProbedDevices);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
