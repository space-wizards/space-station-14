using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NetProbeCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NetProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
    }

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the NetProbe program and someone clicks on something with it. <br/>
    /// <br/>
    /// Saves name, address... etc. of the device that was clicked into a list on the component when the device isn't already present in that list
    /// </summary>
    private void AfterInteract(EntityUid uid, NetProbeCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
            return;

        var target = args.InteractEvent.Target.Value;
        DeviceNetworkComponent? networkComponent = default;

        if (!Resolve(target, ref networkComponent, false))
            return;

        //Ceck if device is already present in list
        foreach (var probedDevice in component.ProbedDevices)
        {
            if (probedDevice.Address == networkComponent.Address)
                return;
        }

        //Play scanning sound with slightly randomized pitch
        //Why is there no NextFloat(float min, float max)???
        var audioParams = AudioParams.Default.WithVolume(-2f).WithPitchScale((float)_random.Next(12, 21) / 10);
        _audioSystem.PlayEntity(component.SoundScan, args.InteractEvent.User, target, audioParams);
        _popupSystem.PopupCursor(Loc.GetString("net-probe-scan", ("device", target)), args.InteractEvent.User);


        //Limit the amount of saved probe results to 9
        //This is hardcoded because the UI doesn't support a dynamic number of results
        if (component.ProbedDevices.Count >= component.MaxSavedDevices)
            component.ProbedDevices.RemoveAt(0);

        var device = new ProbedNetworkDevice(
            Name(target),
            networkComponent.Address,
            networkComponent.ReceiveFrequency?.FrequencyToString() ?? string.Empty,
            networkComponent.DeviceNetId.DeviceNetIdToLocalizedName()
        );

        component.ProbedDevices.Add(device);
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
