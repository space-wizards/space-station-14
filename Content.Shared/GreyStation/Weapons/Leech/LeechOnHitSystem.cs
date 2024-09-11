using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.GreyStation.Weapons.Leech;

public sealed class LeechOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeechOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<LeechOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!HitLivingMob(ent, args.HitEntities))
            return;

        // at least 1 living mob was hit, so heal the user
        // don't scale the healing with number of hit mobs since then wideswing will heal you stupidly fast
        _damageable.TryChangeDamage(args.User, ent.Comp.Leech, true, false, origin: ent);
    }

    private bool HitLivingMob(EntityUid user, IReadOnlyList<EntityUid> entities)
    {
        foreach (var uid in entities)
        {
            if (uid != user && _mobState.IsAlive(uid))
                return true;
        }

        return false;
    }
}
