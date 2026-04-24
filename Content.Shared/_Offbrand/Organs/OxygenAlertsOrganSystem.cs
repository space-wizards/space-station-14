using Content.Shared._Offbrand.Wounds;
using Content.Shared.Alert;
using Content.Shared.Body;

namespace Content.Shared._Offbrand.Organs;

public sealed class OxygenAlertsOrganSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OxygenAlertsOrganComponent, OrganOxygenChangedEvent>(OnOxygenChanged);
        SubscribeLocalEvent<OxygenAlertsOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<OxygenAlertsOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnOxygenChanged(Entity<OxygenAlertsOrganComponent> ent, ref OrganOxygenChangedEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        UpdateAlert(ent, body);
    }

    private void UpdateAlert(Entity<OxygenAlertsOrganComponent> ent, EntityUid target)
    {
        var oxygen = Comp<OxygenatableOrganComponent>(ent);

        if (oxygen.Oxygen == oxygen.MaxOxygen)
        {
            _alerts.ClearAlertCategory(target, ent.Comp.AlertCategory);
            return;
        }

        _alerts.ShowAlert(target, ent.Comp.Alert, severity: (short)oxygen.Oxygen.Int());
    }

    private void OnGotInserted(Entity<OxygenAlertsOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        UpdateAlert(ent, args.Target);
    }

    private void OnGotRemoved(Entity<OxygenAlertsOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        _alerts.ClearAlertCategory(args.Target, ent.Comp.AlertCategory);
    }
}
