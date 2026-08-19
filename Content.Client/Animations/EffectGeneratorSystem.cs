using System.Numerics;
using Content.Shared.Animation;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Animations;

public sealed partial class EffectGeneratorSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<EffectGeneratorComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (_transform.InRange(xform.Coordinates, comp.LastCoordinates, comp.MaxDistance))
            {
                if (_timing.CurTime < comp.NextEffectSpawnTime)
                    continue;
            }

            comp.LastCoordinates = _transform.GetMoverCoordinates(xform.Coordinates);
            comp.NextEffectSpawnTime = _timing.CurTime + comp.EffectCooldown;

            SpawnEffect((uid, comp));
        }
    }

    private void SpawnEffect(Entity<EffectGeneratorComponent> entity)
    {
        var entityXform = Transform(entity);

        var parent = _transform.GetParentUid(entity);
        var speed = HasComp<MapGridComponent>(parent) || HasComp<MapComponent>(parent)
            ? _physics.GetLinearVelocity(entity, Vector2.Zero)
            : _physics.GetLinearVelocity(parent, Vector2.Zero);

        // Don't show particles unless the user is moving.
        if (speed.LengthSquared() < 1f)
            return;

        var coordinates = entityXform.Coordinates;
        var gridUid = _transform.GetGrid(coordinates);

        if (TryComp<MapGridComponent>(gridUid, out var grid))
            coordinates = new EntityCoordinates(gridUid.Value, _mapSystem.WorldToLocal(gridUid.Value, grid, _transform.ToMapCoordinates(coordinates).Position));
        else if (entityXform.MapUid != null)
            coordinates = new EntityCoordinates(entityXform.MapUid.Value, _transform.GetWorldPosition(entityXform));
        else
            return;

        var effect = Spawn(entity.Comp.EffectPrototype, coordinates);

        switch (entity.Comp.RotationPolicy)
        {
            case RotationPolicy.FollowMotionDirection:
                _transform.SetWorldRotation(effect, speed.ToWorldAngle());
                break;

            case RotationPolicy.Random:
                _transform.SetWorldRotation(effect, _random.NextAngle());
                break;
        }
    }
}
