using Content.Server.Explosion.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System;
using System.Numerics;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ScatteringGrenadeSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScatteringGrenadeComponent, ComponentInit>(OnScatteringInit);
        SubscribeLocalEvent<ScatteringGrenadeComponent, ComponentStartup>(OnScatteringStartup);
        SubscribeLocalEvent<ScatteringGrenadeComponent, InteractUsingEvent>(OnScatteringInteractUsing);
        SubscribeLocalEvent<ScatteringGrenadeComponent, TriggerEvent>(OnScatteringTrigger);
    }

    private void OnScatteringInit(Entity<ScatteringGrenadeComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _container.EnsureContainer<Container>(entity.Owner, "cluster-payload");
    }

    /// <summary>
    /// Setting the unspawned count based on capacity so we know how many new entities to spawn
    /// Update appearance based on initial fill amount
    /// </summary>
    private void OnScatteringStartup(Entity<ScatteringGrenadeComponent> entity, ref ComponentStartup args)
    {
        if (entity.Comp.FillPrototype == null)
            return;

        entity.Comp.UnspawnedCount = Math.Max(0, entity.Comp.Capacity - entity.Comp.Container.ContainedEntities.Count);
        UpdateAppearance(entity);
    }

    /// <summary>
    /// There are some scattergrenades you can fill up with more grenades (like clusterbangs)
    /// This covers how you insert more into it
    /// </summary>
    private void OnScatteringInteractUsing(Entity<ScatteringGrenadeComponent> entity, ref InteractUsingEvent args)
    {
        if (entity.Comp.Whitelist == null)
            return;

        if (args.Handled || !_whitelistSystem.IsValid(entity.Comp.Whitelist, args.Used))
            return;

        _container.Insert(args.Used, entity.Comp.Container);
        UpdateAppearance(entity);
        args.Handled = true;
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
            if (component.IsTriggered)
            {
                if (totalCount > 0)
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

    /// <summary>
    /// Update appearance based off of total count of contents
    /// </summary>
    private void UpdateAppearance(Entity<ScatteringGrenadeComponent> entity)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearanceComponent))
            return;

        _appearance.SetData(entity, ClusterGrenadeVisuals.GrenadesCounter, entity.Comp.UnspawnedCount + entity.Comp.Container.ContainedEntities.Count, appearanceComponent);
    }
}
