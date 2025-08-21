using Content.Server.Medical.Components;
using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Server.Medical;

public sealed class VomitOnDamageSystem : EntitySystem
{
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VomitOnDamageComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnDamage(Entity<VomitOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.DamageDelta == null)
            return;

        if (ent.Comp.NextVomitTime > _gameTiming.CurTime)
            return;

        var vomited = false;

        foreach (var (type, _) in args.DamageDelta.DamageDict)
        {
            if (!ent.Comp.Damage.TryGetValue(type, out var threshold))
                continue;

            if (threshold != null && (!args.Damageable.Damage.DamageDict.TryGetValue(type, out var total) || threshold > total))
                continue;

            _vomitSystem.Vomit(ent);
            vomited = true;
        }

        if (vomited)
            ent.Comp.NextVomitTime = _gameTiming.CurTime + ent.Comp.VomitCooldown;
    }
}
