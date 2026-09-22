using System.Linq;
using System.Numerics;
using Content.Server.ImmovableRod;
using Content.Server.StationEvents.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn an immovable rod at a random point and toss it in a random direction.
/// </summary>
/// <seealso cref="ImmovableRodRuleComponent"/>
public sealed partial class ImmovableRodRule : StationEventSystem<ImmovableRodRuleComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    protected override void Started(Entity<ImmovableRodRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var protoName = EntitySpawnCollection.GetSpawns(ent.Comp1.RodPrototypes).First();

        var proto = _prototypeManager.Index<EntityPrototype>(protoName);

        if (proto.TryComp<ImmovableRodComponent>(out var rod, EntityManager.ComponentFactory) &&
            proto.TryComp<TimedDespawnComponent>(out var despawn, EntityManager.ComponentFactory))
        {
            if (!Station.TryFindRandomTile(out _, out _, out _, out var targetCoords))
                return;

            var speed = RobustRandom.NextFloat(rod.MinSpeed, rod.MaxSpeed);
            var angle = RobustRandom.NextAngle();
            var direction = angle.ToVec();
            var mapCoords = _transform.ToMapCoordinates(targetCoords);
            var spawnCoords = mapCoords.Offset(-direction * speed * despawn.Lifetime / 2);
            var rodUid = Spawn(protoName, spawnCoords);
            _gun.ShootProjectile(ent, direction, Vector2.Zero, rodUid, speed: speed);
        }
        else
        {
            Sawmill.Error($"Invalid immovable rod prototype: {protoName}");
        }
    }
}
