using System.Linq;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public sealed class ShockAlertsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PainSystem _pain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockAlertsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShockAlertsComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ShockAlertsComponent, AfterShockChangeEvent>(OnAfterShockChange);
    }

    private ProtoId<AlertPrototype>? DetermineThreshold(Entity<ShockAlertsComponent> ent)
    {
        var shock = FixedPoint2.Max(_pain.GetShock(ent.Owner), FixedPoint2.Zero);

        if (Comp<PainComponent>(ent).Suppressed)
            return ent.Comp.SuppressedAlert;

        return ent.Comp.Thresholds.HighestMatch(shock);
    }

    private void UpdateAlert(Entity<ShockAlertsComponent> ent)
    {
        var targetEffect = DetermineThreshold(ent);
        if (targetEffect == ent.Comp.CurrentThresholdState)
            return;

        ent.Comp.CurrentThresholdState = targetEffect;

        if (targetEffect is { } effect)
        {
            _alerts.ShowAlert(ent.Owner, effect);
        }
        else
        {
            _alerts.ClearAlertCategory(ent.Owner, ent.Comp.AlertCategory);
        }
    }

    private void OnMapInit(Entity<ShockAlertsComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnComponentShutdown(Entity<ShockAlertsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent.Owner, ent.Comp.AlertCategory);
    }

    private void OnAfterShockChange(Entity<ShockAlertsComponent> ent, ref AfterShockChangeEvent args)
    {
        UpdateAlert(ent);
    }
}
