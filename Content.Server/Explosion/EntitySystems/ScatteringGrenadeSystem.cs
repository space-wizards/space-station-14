using Content.Shared.Explosion.Components;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Numerics;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ScatteringGrenadeSystem : SharedScatteringGrenadeSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScatteringGrenadeComponent, TriggerEvent>(OnScatteringTrigger);
    }

    /// <summary>
    /// Can be triggered either by damage or the use in hand timer, either way
    /// will store the event happening in IsTriggered for the next frame update rather than
    /// handling it here to prevent crashing the game
    /// </summary>
    private void OnScatteringTrigger(Entity<ScatteringGrenadeComponent> entity, ref TriggerEvent args)
    {
        entity.Comp.IsTriggered = true;
        args.Handled = true;
    }

    /// <summary>
    /// Every frame update we look for scattering grenades that were triggered (by damage or timer)
    /// Then we spawn the contents, throw them, optionally trigger them, then delete the original scatter grenade entity
    /// </summary>
    public override void Update(float frametime)
    {
        base.Update(frametime);
        var query = EntityQueryEnumerator<ScatteringGrenadeComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            var totalCount = component.Container.ContainedEntities.Count + component.UnspawnedCount;

            // if triggered while empty, (if it's blown up while empty) it'll just delete itself
            if (component.IsTriggered && totalCount > 0)
            {
                var grenadeCoord = _transformSystem.GetMapCoordinates(uid);
                var thrownCount = 0;
                var segmentAngle = 360 / totalCount;
                var additionalIntervalDelay = 0f;

                while (TrySpawnContents(grenadeCoord, component, out var contentUid))
                {
                    Angle angle;
                    if (component.RandomAngle)
                        angle = _random.NextAngle();
                    else
                    {
                        var angleMin = segmentAngle * thrownCount;
                        var angleMax = segmentAngle * (thrownCount + 1);
                        angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));
                        thrownCount++;
                    }

                    Vector2 direction = angle.ToVec().Normalized();
                    if (component.RandomDistance)
                        direction *= _random.NextFloat(component.RandomThrowDistanceMin, component.RandomThrowDistanceMax);
                    else
                        direction *= component.Distance;

                    _throwingSystem.TryThrow(contentUid, direction, component.Velocity);

                    if (component.TriggerContents)
                    {
                        additionalIntervalDelay += _random.NextFloat(component.IntervalBetweenTriggersMin, component.IntervalBetweenTriggersMax);
                        var contentTimer = EnsureComp<ActiveTimerTriggerComponent>(contentUid);
                        contentTimer.TimeRemaining = component.DelayBeforeTriggerContents + additionalIntervalDelay;
                        var ev = new ActiveTimerTriggerEvent(contentUid, uid);
                        RaiseLocalEvent(contentUid, ref ev);
                    }
                }

                // Normally we'd use DeleteOnTrigger but because we need to wait for the frame update
                // we have to delete it here instead
                Del(uid);
            }
        }
    }

    /// <summary>
    /// Spawns one instance of the fill prototype or contained entity at the coordinate indicated
    /// </summary>
    private bool TrySpawnContents(MapCoordinates spawnCoordinates, ScatteringGrenadeComponent component, out EntityUid contentUid)
    {
        contentUid = default;

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            contentUid = Spawn(component.FillPrototype, spawnCoordinates);
            return true;
        }

        if (component.Container.ContainedEntities.Count > 0)
        {
            contentUid = component.Container.ContainedEntities[0];

            if (!_container.Remove(contentUid, component.Container))
                return false;

            return true;
        }

        return false;
    }
}
