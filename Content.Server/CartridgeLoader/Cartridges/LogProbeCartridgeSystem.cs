using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class LogProbeCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
    }

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the LogProbe program and someone clicks on something with it. <br/>
    /// <br/>
    /// Updates the program's list of logs with those from the device.
    /// </summary>
    private void AfterInteract(EntityUid uid, LogProbeCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
            return;

        var target = args.InteractEvent.Target.Value;
        AccessReaderComponent? accessReaderComponent = default;

        if (!Resolve(target, ref accessReaderComponent, false))
            return;

        //Play scanning sound with slightly randomized pitch
        var audioParams = AudioParams.Default.WithVolume(-2f).WithPitchScale((float)_random.Next(12, 21) / 10);
        _audioSystem.PlayEntity(component.SoundScan, args.InteractEvent.User, target, audioParams);
        _popupSystem.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.InteractEvent.User);

        component.PulledAccessLogs.Clear();

        foreach (var accessRecord in accessReaderComponent.AccessLog)
        {
            var log = new PulledAccessLog(
                accessRecord.AccessTime,
                accessRecord.Accessor
            );

            component.PulledAccessLogs.Add(log);
        }

        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, LogProbeCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, LogProbeCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new LogProbeUiState(component.PulledAccessLogs);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
