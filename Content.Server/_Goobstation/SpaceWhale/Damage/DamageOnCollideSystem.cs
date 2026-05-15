using Content.Shared.Damage.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._Goobstation.SpaceWhale.Damage;

/// <summary>
/// Урон на коллизию с двумя предохранителями:
///   * Cooldown — per-target минимальный интервал.
///   * RequirePushing — для сегментов кита: бьём только если сегмент реально
///     упирается (выставляется TailedEntitySystem).
/// </summary>
public sealed partial class DamageOnCollideSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOnCollideComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<DamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        var target = ent.Comp.Inverted ? args.OtherEntity : ent.Owner;
        if (target == ent.Owner || Deleted(target))
            return;

        // Не бить себя и своих сегментов хвоста.
        if (HasComp<WhaleSpawnedByComponent>(target) || HasComp<SpaceWhaleSegmentComponent>(target))
            return;

        if (ent.Comp.RequirePushing
            && TryComp<SpaceWhaleSegmentComponent>(ent.Owner, out var seg)
            && !seg.IsPushing)
            return;

        var now = _timing.CurTime;
        if (ent.Comp.Cooldown > 0f
            && ent.Comp.NextHit.TryGetValue(target, out var until)
            && now < until)
            return;

        _damageable.TryChangeDamage(target, ent.Comp.Damage, origin: ent.Owner);

        if (ent.Comp.Cooldown > 0f)
            ent.Comp.NextHit[target] = now + TimeSpan.FromSeconds(ent.Comp.Cooldown);
    }
}
