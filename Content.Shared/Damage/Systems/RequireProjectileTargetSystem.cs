using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Systems;

public sealed class RequireProjectileTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(Entity<RequireProjectileTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
          return;

        if (!RequiresExplicitTarget(ent))
            return;

        var other = args.OtherEntity;
        if (!TryComp(other, out ProjectileComponent? projectile) ||
            CompOrNull<TargetedProjectileComponent>(other)?.Target == ent)
        {
            return;
        }

        // Keep projectiles colliding with the container when firing from inside one.
        var shooter = projectile.Shooter;
        if (shooter is { } shooterUid &&
            !TerminatingOrDeleted(shooterUid) &&
            _container.IsEntityOrParentInContainer(shooterUid))
        {
            return;
        }

        args.Cancelled = true;
    }

    /// <summary>
    /// Uses standing state for mobs and the explicit flag for entities without one.
    /// </summary>
    public bool RequiresExplicitTarget(Entity<RequireProjectileTargetComponent> ent)
    {
        // Mob and standing state are networked separately. Incapacitated mobs may briefly
        // retain a stale standing state on the client, but must still be ignored unless aimed at.
        if (TryComp<MobStateComponent>(ent, out var mobState) &&
            mobState.CurrentState is MobState.PreCritical or MobState.Critical or MobState.Dead)
            return true;

        if (TryComp<StandingStateComponent>(ent, out var standing))
            return !standing.Standing;

        return ent.Comp.Active;
    }

    /// <summary>
    /// Conservatively handles Active and Standing arriving in separate client states.
    /// This must not be used for authoritative collision decisions.
    /// </summary>
    public bool RequiresExplicitTargetForPrediction(Entity<RequireProjectileTargetComponent> ent)
    {
        return ent.Comp.Active || RequiresExplicitTarget(ent);
    }

    private void SetActive(Entity<RequireProjectileTargetComponent> ent, bool value)
    {
        if (ent.Comp.Active == value)
            return;

        ent.Comp.Active = value;
        Dirty(ent);
    }

    private void StandingBulletHit(Entity<RequireProjectileTargetComponent> ent, ref StoodEvent args)
    {
        SetActive(ent, false);
    }

    private void LayingBulletPass(Entity<RequireProjectileTargetComponent> ent, ref DownedEvent args)
    {
        SetActive(ent, true);
    }
}
