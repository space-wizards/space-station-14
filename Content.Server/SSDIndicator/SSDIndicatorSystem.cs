using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

/// <summary>
///     Handles displaying SSD indicator as status icon
/// </summary>
public sealed class SSDIndicatorSystem : SharedSSDIndicatorSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private bool _icSsdSleep;
    private float _icSsdSleepTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<VisitingMindComponent, PlayerDetachedEvent>(OnVisitingDetached);

        _cfg.OnValueChanged(CCVars.ICSSDSleep, obj => _icSsdSleep = obj, true);
        _cfg.OnValueChanged(CCVars.ICSSDSleepTime, obj => _icSsdSleepTime = obj, true);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        // If the mind is only visiting another entity, don't mark it as SSD.
        var disconnecting = args.Player.State.Status == SessionStatus.Disconnected;
        _mind.TryGetMind(uid, out _, out var mind);

        component.IsSSD = mind == null || disconnecting || !mind.IsVisitingEntity;

        // Sets the time when the entity should fall asleep
        if (_icSsdSleep)
        {
            component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        }

        Dirty(uid, component);
    }

    private void OnVisitingDetached(EntityUid uid, VisitingMindComponent component, PlayerDetachedEvent args)
    {
        if (!_mind.TryGetMind(uid, out _, out var mind) || mind.OwnedEntity == null)
            return;

        if (!TryComp<SSDIndicatorComponent>(mind.OwnedEntity.Value, out var ssd))
            return;

        var disconnecting = args.Player.State.Status == SessionStatus.Disconnected;

        ssd.IsSSD = disconnecting || !mind.IsVisitingEntity;

        // Sets the time when the entity should fall asleep
        if (_icSsdSleep)
        {
            ssd.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_icSsdSleepTime);
        }

        Dirty(mind.OwnedEntity.Value, ssd);
    }
}
