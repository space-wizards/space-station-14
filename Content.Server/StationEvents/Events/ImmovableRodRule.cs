using System.Numerics;
using Content.Server.StationEvents.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;
using System.Linq;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.StationEvents.Events;

public sealed class ImmovableRodRule : StationEventSystem<ImmovableRodRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    protected override void Started(EntityUid uid,
        ImmovableRodRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        LaunchRod((uid, component));
    }

    private void LaunchRod(Entity<ImmovableRodRuleComponent> ent)
    {
        // TryFindRandomTile will attempt to find a good target for the Rod.
        // Due to the way this check works, the tile won't be airtight or spaced.
        if (!TryFindRandomTile(out _, out _, out var targetGrid, out var targetCoords))
            return;

        // If the target grid is valid, see if any mobs are on that grid. If there are, pick one at random.
        // This helps ensure that the rod will go where some mob has been recently, making it more likely
        // to be interesting.
        if (targetGrid.IsValid())
        {
            // Get every single mob...
            var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
            while (query.MoveNext(out var _, out var mob, out var transform))
            {
                // This grabs every mob, so ignore any that are not on the "good" grid we got from TryFindRandomTile.
                if (transform.GridUid != targetGrid)
                    continue;

                // Don't target stuff that can't run away, that's just mean.
                if (mob.CurrentState is not MobState.Alive)
                    continue;

                // TODO: If this is still not interesting enough, try and look for mobs that have players attached
                // so we don't target an SSD Pun Pun.

                targetCoords = transform.Coordinates;

                break;
            }
        }

        // Picks some arbitrary direction. The rod will be spawned in such a way that the rod will fly on this
        // vector before colliding with the target coordinates after PreTargetLifespan seconds.
        var direction = RobustRandom.NextAngle().ToVec();
        var rodSpawnOffset = -direction * ent.Comp.LaunchSpeed * ent.Comp.PreTargetLifespan.Seconds;
        var spawnCoords = _transform.ToMapCoordinates(targetCoords).Offset(rodSpawnOffset);

        var rodEnt = Spawn(EntitySpawnCollection.GetSpawns(ent.Comp.RodPrototypes).First(), spawnCoords);
        // The projectile will cease to exist after its total lifespan.
        AddComp<TimedDespawnComponent>(rodEnt).Lifetime = ent.Comp.TotalLifespanSeconds;

        // Fire the rod as a projecitle.
        _gun.ShootProjectile(rodEnt, direction, Vector2.Zero, ent, speed: ent.Comp.LaunchSpeed);
    }
}
