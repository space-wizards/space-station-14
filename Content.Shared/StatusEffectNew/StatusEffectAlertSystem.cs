using Content.Shared.Alert;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handles displaying status effects that should show an alert, optionally with a duration.
/// </summary>
public sealed class StatusEffectAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private EntityQuery<StatusEffectComponent> _effectQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectAlertComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<StatusEffectAlertComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<StatusEffectAlertComponent, StatusEffectEndTimeUpdatedEvent>(OnEndTimeUpdated);

        _effectQuery = GetEntityQuery<StatusEffectComponent>();
    }

    private void OnStatusEffectApplied(Entity<StatusEffectAlertComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!_effectQuery.TryComp(ent, out var effectComp))
            return;

        _alerts.UpdateAlert(args.Target, ent.Comp.Alert, cooldown: ent.Comp.ShowDuration ? effectComp.EndEffectTime : null);
    }

    private void OnStatusEffectRemoved(Entity<StatusEffectAlertComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _alerts.ClearAlert(args.Target, ent.Comp.Alert);
    }

    private void OnEndTimeUpdated(Entity<StatusEffectAlertComponent> ent, ref StatusEffectEndTimeUpdatedEvent args)
    {
        _alerts.UpdateAlert(args.Target, ent.Comp.Alert, cooldown: ent.Comp.ShowDuration ? args.EndTime : null);
    }
}
