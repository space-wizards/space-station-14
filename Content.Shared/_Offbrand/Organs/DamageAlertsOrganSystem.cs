using Content.Shared._Offbrand.Wounds;
using Content.Shared.Alert;
using Content.Shared.Body;

namespace Content.Shared._Offbrand.Organs;

public sealed class DamageAlertsOrganSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageAlertsOrganComponent, OrganDamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DamageAlertsOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<DamageAlertsOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnDamageChanged(Entity<DamageAlertsOrganComponent> ent, ref OrganDamageChangedEvent args)
    {
        var lungDamage = Comp<DamageableOrganComponent>(ent);
        var targetAlert = ent.Comp.AlertThresholds.HighestMatch(lungDamage.Damage);

        if (targetAlert == ent.Comp.CurrentAlertThresholdState)
            return;

        ent.Comp.CurrentAlertThresholdState = targetAlert;
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        if (targetAlert is { } alert)
        {
            _alerts.ShowAlert(body, alert);
        }
        else
        {
            _alerts.ClearAlertCategory(body, ent.Comp.AlertCategory);
        }
    }

    private void OnGotInserted(Entity<DamageAlertsOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (ent.Comp.CurrentAlertThresholdState is { } alert)
            _alerts.ShowAlert(args.Target, alert);
    }

    private void OnGotRemoved(Entity<DamageAlertsOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        _alerts.ClearAlertCategory(args.Target, ent.Comp.AlertCategory);
    }
}
