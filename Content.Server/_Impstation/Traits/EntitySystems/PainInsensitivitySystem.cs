using Content.Shared.Alert;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Impstation.Traits;

public sealed partial class PainInsensitivitySystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    [ValidatePrototypeId<AlertCategoryPrototype>]
    private const string HealthAlertCategoryProto = "Health";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PainInsensitivityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PainInsensitivityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, PainInsensitivityComponent comp, ComponentStartup args)
    {
        if (comp.LifeStage != ComponentLifeStage.Starting)
            return;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            Log.Warning("PainInsensitivity attached to entity with no MobThresholds. Removing...");
            RemComp(uid, comp);
            return;
        }
        comp.OriginalShowOverlays = thresholds.ShowOverlays;
        comp.OriginalShowBruteOverlay = thresholds.ShowBruteOverlay;
        comp.OriginalShowAirlossOverlay = thresholds.ShowAirlossOverlay;
        comp.OriginalShowCritOverlay = thresholds.ShowCritOverlay;
        comp.OriginalTriggersAlerts = thresholds.TriggersAlerts;

        _thresholds.SetOverlaysEnabled(uid, comp.ShowOverlays, thresholds);
        _thresholds.SetBruteOverlayEnabled(uid, comp.ShowBruteOverlay, thresholds);
        _thresholds.SetAirlossOverlayEnabled(uid, comp.ShowAirlossOverlay, thresholds);
        _thresholds.SetCritOverlayEnabled(uid, comp.ShowCritOverlay, thresholds);
        _thresholds.SetTriggersAlerts(uid, comp.TriggersAlerts, thresholds);

        if(!comp.TriggersAlerts)
            _alerts.ClearAlertCategory(uid, HealthAlertCategoryProto);
    }

    private void OnShutdown(EntityUid uid, PainInsensitivityComponent comp, ComponentShutdown args)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        _thresholds.SetOverlaysEnabled(uid, comp.OriginalShowOverlays, thresholds);
        _thresholds.SetBruteOverlayEnabled(uid, comp.OriginalShowBruteOverlay, thresholds);
        _thresholds.SetAirlossOverlayEnabled(uid, comp.OriginalShowAirlossOverlay, thresholds);
        _thresholds.SetCritOverlayEnabled(uid, comp.OriginalShowCritOverlay, thresholds);
    }
}
