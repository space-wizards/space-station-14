using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Damage.Systems;

public sealed class DamageOnHoldingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnHoldingComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    public void SetEnabled(EntityUid uid, bool enabled, DamageOnHoldingComponent? component = null)
    {
        if (Resolve(uid, ref component))
            component.Enabled = enabled;
    }

    private void OnUnpaused(EntityUid uid, DamageOnHoldingComponent component, ref EntityUnpausedEvent args)
    {
        component.NextDamage += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DamageOnHoldingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Enabled || component.NextDamage > _timing.CurTime)
                continue;
            if (_container.TryGetContainingContainer(uid, out var container))
            {
                _damageableSystem.TryChangeDamage(container.Owner, component.Damage, origin: uid);
            }
            component.NextDamage += TimeSpan.FromSeconds(component.Interval);
            if (component.NextDamage < _timing.CurTime) // on first iteration or if component was disabled for long time
                component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(component.Interval);
        }
    }
}