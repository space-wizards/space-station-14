using Content.Shared.Bed.Sleep;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        component.IsSSD = false;

        // Removes force sleep and resets the time to zero
        if (_cfg.GetCVar(CCVars.ICSSDSleep))
        {
            component.FallAsleepTime = TimeSpan.Zero;
            EntityManager.RemoveComponent<ForcedSleepingComponent>(uid);
        }
        Dirty(uid, component);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        component.IsSSD = true;

        // Sets the time when the entity should fall asleep
        if (_cfg.GetCVar(CCVars.ICSSDSleep))
        {
            component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.ICSSDSleepTime));
        }
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_cfg.GetCVar(CCVars.ICSSDSleep))
            return;

        var query = EntityQueryEnumerator<SSDIndicatorComponent>();

        while (query.MoveNext(out var uid, out var ssd))
        {
            // Forces the entity to sleep when the time has come
            if(ssd.IsSSD &&
                ssd.FallAsleepTime <= _timing.CurTime &&
                !TerminatingOrDeleted(uid))
            {
                EnsureComp<ForcedSleepingComponent>(uid);
            }
        }
    }
}
