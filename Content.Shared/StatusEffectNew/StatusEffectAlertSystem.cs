using Content.Shared.Alert;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handles displaying status effects that should show an alert, optionally with a duration.
/// </summary>
public sealed class StatusEffectAlertSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
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

        RefreshAlert(ent, args.Target, effectComp.EndEffectTime);
    }

    private void OnStatusEffectRemoved(Entity<StatusEffectAlertComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _alerts.ClearAlert(args.Target, ent.Comp.Alert);
    }

    private void OnEndTimeUpdated(Entity<StatusEffectAlertComponent> ent, ref StatusEffectEndTimeUpdatedEvent args)
    {
        RefreshAlert(ent, args.Target, args.EndTime);
    }

    private void RefreshAlert(Entity<StatusEffectAlertComponent> ent, EntityUid target, TimeSpan? endTime)
    {
        (TimeSpan Start, TimeSpan End)? cooldown = null;

        // Make sure the start time of the alert cooldown is still accurate
        // This ensures the progress wheel doesn't "reset" every duration change.
        if (ent.Comp.ShowDuration
            && endTime is not null
            && _alerts.TryGet(ent.Comp.Alert, out var alert))
        {
            _alerts.TryGetAlertState(target, alert.AlertKey, out var alertState);
            cooldown = (alertState.Cooldown?.Item1 ?? _timing.CurTime, endTime.Value);
        }

        _alerts.ShowAlert(target, ent.Comp.Alert, cooldown: cooldown);
    }
}
