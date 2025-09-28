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
            var strain = FixedPoint2.Min(_heart.HeartStrain((ent, heartrate)), ent.Comp.MaxStrain);
            _alerts.ShowAlert(ent.Owner, ent.Comp.StrainAlert, (short)strain.Int());
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
