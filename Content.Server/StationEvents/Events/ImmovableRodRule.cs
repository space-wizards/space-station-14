using System.Numerics;
using Content.Server.ImmovableRod;
using Content.Server.StationEvents.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
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
    [Dependency] private EntityTableSystem _entityTable = default!;

    protected override void Started(Entity<ImmovableRodRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        foreach (var protoName in _entityTable.GetSpawns(rule.Comp1.RodPrototypes))
        {
            var proto = ProtoMan.Index(protoName);

            if (!proto.TryComp<ImmovableRodComponent>(out var rodComp, Factory) ||
                !proto.TryComp<TimedDespawnComponent>(out var despawn, Factory))
            {
                Sawmill.Error($"Invalid immovable rod prototype: {protoName}");
                continue;
            }

            if (!Station.TryFindRandomTile(out _, out _, out _, out var targetCoords))
                return;

            var speed = RobustRandom.NextFloat(rodComp.MinSpeed, rodComp.MaxSpeed);
            var angle = RobustRandom.NextAngle();
            var direction = angle.ToVec();
            var mapCoords = _transform.ToMapCoordinates(targetCoords);
            var spawnCoords = mapCoords.Offset(-direction * speed * despawn.Lifetime / 2);
            var rodUid = Spawn(protoName, spawnCoords);
            _gun.ShootProjectile(rodUid, direction, Vector2.Zero, rule, speed: speed);
        }
    }
}
