using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.CartridgeLoader.Cartridges;

public sealed class NetProbeCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeRelayedEvent<AfterInteractEvent>>(AfterInteract);
    }

    /// <summary>
    /// Saves name, address... etc. of the device that was clicked into a list on the component when the device isn't already present in that list
    /// </summary>
    private void AfterInteract(EntityUid uid, NetProbeCartridgeComponent component, CartridgeRelayedEvent<AfterInteractEvent> args)
    {
        if (args.Args.Handled || !args.Args.CanReach || !args.Args.Target.HasValue)
            return;

        var target = args.Args.Target.Value;

        if (!TryComp<DeviceNetworkComponent>(target, out var networkComponent))
            return;

        // Check if device is already present in list
        foreach (var probedDevice in component.ProbedDevices)
        {
            if (probedDevice.Address == networkComponent.Address)
                return;
        }

        // Play scanning sound with slightly randomized pitch
        // Why is there no NextFloat(float min, float max)???
        var audioParams = AudioParams.Default.WithVolume(-2f).WithVariation(0.2f);
        _audioSystem.PlayPredicted(component.SoundScan, target, args.Args.User, audioParams);
        _popupSystem.PopupPredictedCursor(Loc.GetString("net-probe-scan", ("device", target)), args.Args.User);

        // Limit the amount of saved probe results to 9
        // This is hardcoded because the UI doesn't support a dynamic number of results
        if (component.ProbedDevices.Count >= component.MaxSavedDevices)
            component.ProbedDevices.RemoveAt(0);

        var device = new ProbedNetworkDevice(
            Name(target),
            networkComponent.Address,
            networkComponent.ReceiveFrequency?.FrequencyToString() ?? string.Empty,
            networkComponent.DeviceNetId.DeviceNetIdToLocalizedName()
        );

        component.ProbedDevices.Add(device);
        Dirty(uid, component);
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
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
