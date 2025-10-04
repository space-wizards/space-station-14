using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed class LungDamageSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LungDamageComponent, BeforeBreathEvent>(OnBeforeBreath);
        SubscribeLocalEvent<LungDamageComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<LungDamageAlertsComponent, AfterLungDamageChangedEvent>(OnLungDamageChanged);
        SubscribeLocalEvent<LungDamageAlertsComponent, ComponentShutdown>(OnAlertsShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LungDamageComponent, PassiveLungDamageComponent>();
        while (query.MoveNext(out var uid, out var lung, out var passive))
        {
            if (_timing.CurTime < passive.NextUpdate)
                continue;

            passive.NextUpdate = _timing.CurTime + passive.UpdateInterval;
            Dirty(uid, passive);

            if (lung.Damage > passive.DamageCap)
                continue;

            TryModifyDamage((uid, lung), passive.Damage);
        }
    }

    private void OnBeforeBreath(Entity<LungDamageComponent> ent, ref BeforeBreathEvent args)
    {
        args.BreathVolume *= 1f - MathF.Pow(ent.Comp.Damage.Float() / ent.Comp.MaxDamage.Float(), 3f);
    }

    public void TryModifyDamage(Entity<LungDamageComponent?> ent, FixedPoint2 damage)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Damage = FixedPoint2.Clamp(ent.Comp.Damage + damage, FixedPoint2.Zero, ent.Comp.MaxDamage);
        Dirty(ent);

        var evt = new AfterLungDamageChangedEvent();
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnRejuvenate(Entity<LungDamageComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Damage = FixedPoint2.Zero;
        Dirty(ent);

        var evt = new AfterLungDamageChangedEvent();
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnLungDamageChanged(Entity<LungDamageAlertsComponent> ent, ref AfterLungDamageChangedEvent args)
    {
        var lungDamage = Comp<LungDamageComponent>(ent);
        var targetAlert = ent.Comp.AlertThresholds.HighestMatch(lungDamage.Damage);

        if (targetAlert == ent.Comp.CurrentAlertThresholdState)
            return;

        if (targetAlert is { } alert)
        {
            _alerts.ShowAlert(ent.Owner, alert);
        }
        else
        {
            _alerts.ClearAlertCategory(ent.Owner, ent.Comp.AlertCategory);
        }
    }

    private void OnAlertsShutdown(Entity<LungDamageAlertsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent.Owner, ent.Comp.AlertCategory);
    }
}
