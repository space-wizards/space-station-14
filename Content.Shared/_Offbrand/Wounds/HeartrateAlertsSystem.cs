using Content.Shared.Alert;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Wounds;

public sealed class HeartrateAlertsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HeartSystem _heart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartrateAlertsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HeartrateAlertsComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<HeartrateAlertsComponent, AfterStrainChangedEvent>(OnAfterStrainChanged);
        SubscribeLocalEvent<HeartrateAlertsComponent, HeartStoppedEvent>(OnHeartStopped);
        SubscribeLocalEvent<HeartrateAlertsComponent, HeartStartedEvent>(OnHeartStarted);
    }

    private void UpdateAlert(Entity<HeartrateAlertsComponent> ent)
    {
        var heartrate = Comp<HeartrateComponent>(ent);
        if (heartrate.Running)
        {
            var range = _alerts.GetSeverityRange(ent.Comp.StrainAlert);
            var min = _alerts.GetMinSeverity(ent.Comp.StrainAlert);
            var max = _alerts.GetMaxSeverity(ent.Comp.StrainAlert);

            var severity = Math.Min(min + (short)Math.Round(range * _heart.Strain((ent.Owner, heartrate))), max);
            _alerts.ShowAlert(ent.Owner, ent.Comp.StrainAlert, (short)severity);
        }
        else
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp.StoppedAlert);
        }
    }

    private void OnMapInit(Entity<HeartrateAlertsComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnAfterStrainChanged(Entity<HeartrateAlertsComponent> ent, ref AfterStrainChangedEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnComponentShutdown(Entity<HeartrateAlertsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent.Owner, ent.Comp.AlertCategory);
    }

    private void OnHeartStopped(Entity<HeartrateAlertsComponent> ent, ref HeartStoppedEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnHeartStarted(Entity<HeartrateAlertsComponent> ent, ref HeartStartedEvent args)
    {
        UpdateAlert(ent);
    }
}
