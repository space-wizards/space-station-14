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
        SubscribeLocalEvent<SSDIndicatorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        component.IsSSD = false;

        // Removes force sleep and resets the time to zero
        if (_cfg.GetCVar(CCVars.ICSSDSleep))
        {
            component.FallAsleepTime = TimeSpan.Zero;
            if (component.ForcedSleepAdded) // Remove component only if it has been added by this system
            {
                EntityManager.RemoveComponent<ForcedSleepingComponent>(uid);
                component.ForcedSleepAdded = false;
            }
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

    // Prevents mapped mobs to go to sleep immediately
    private void OnMapInit(EntityUid uid, SSDIndicatorComponent component, MapInitEvent args)
    {
        if (_cfg.GetCVar(CCVars.ICSSDSleep) &&
            component.IsSSD &&
            component.FallAsleepTime == TimeSpan.Zero)
        {
            component.FallAsleepTime = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.ICSSDSleepTime));
        }
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
                !TerminatingOrDeleted(uid) &&
                !HasComp<ForcedSleepingComponent>(uid)) // Don't add the component if the entity has it from another sources
            {
                EnsureComp<ForcedSleepingComponent>(uid);
                ssd.ForcedSleepAdded = true;
            }
        }
    }
}
