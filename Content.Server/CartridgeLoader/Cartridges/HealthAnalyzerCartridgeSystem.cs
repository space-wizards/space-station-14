using Content.Server.Body.Components;
using Content.Server.Medical.Components;
using Content.Server.Temperature.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.MedicalScanner;

namespace Content.Server.CartridgeLoader.Cartridges;

/// <summary>
/// Handles install and removal of Health Analyzer Cartridge into a PDA
/// </summary>
public sealed class HealthAnalyzerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealthAnalyzerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<HealthAnalyzerCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<HealthAnalyzerCartridgeComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
    }

    private void AfterInteract(EntityUid uid, HealthAnalyzerCartridgeComponent component, CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || !args.InteractEvent.Target.HasValue)
            return;

        _audio.PlayPvs(component.ScanningBeginSound, uid);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.Loader, component.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.InteractEvent.Target, used: args.Loader)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false
        });
    }

    private void OnDoAfter(EntityUid uid, HealthAnalyzerCartridgeComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        _audio.PlayPvs(component.ScanningEndSound, uid);

        UpdateScannedUser(uid, args.Args.User, args.Args.Target, component);
        args.Handled = true;
    }

    private void OnUiReady(EntityUid uid, HealthAnalyzerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateScannedUser(uid, args.Loader, null, component);
    }

    private void UpdateScannedUser(EntityUid uid, EntityUid loaderUid, EntityUid? target, HealthAnalyzerCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!HasComp<DamageableComponent>(target))
            return;

        TryComp<TemperatureComponent>(target, out var temp);
        TryComp<BloodstreamComponent>(target, out var bloodstream);

        var scannedUserMessage = new HealthAnalyzerScannedUserMessage(target,
            temp?.CurrentTemperature ?? float.NaN,
            bloodstream != null ? bloodstream.BloodSolution.FillFraction : float.NaN);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, new HealthAnalyzerUiState(scannedUserMessage));
    }
}
