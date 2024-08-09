using Content.Shared.Bed.Sleep;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Shared.SSDIndicator;

/// <summary>
///     Handle changing player SSD indicator status
/// </summary>
public sealed class SSDIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        component.IsSSD = false;
        if (_cfg.GetCVar(CCVars.ICSSDSleep))
        {
            EntityManager.RemoveComponent<ForcedSleepingComponent>(uid);
        }
        Dirty(uid, component);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        component.IsSSD = true;
        if (_cfg.GetCVar(CCVars.ICSSDSleep) && !TerminatingOrDeleted(uid))
        {
            EnsureComp<ForcedSleepingComponent>(uid);
        }
        Dirty(uid, component);
    }
}
