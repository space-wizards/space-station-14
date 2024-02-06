using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing.Systems;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    //TODO: (future optimization) implement a system that "caps" giblet entities by deleting the oldest ones once we reach a certain limit, customizable via CVAR


    /// <summary>
    /// Attempt to gib a specified entity. That entity must have a gibable components. This method is NOT recursive will only
    /// work on the target and any entities it contains (depending on gibContentsOption)
    /// </summary>
    /// <param name="outerEntity">The outermost entity we care about, used to place the dropped items</param>
    /// <param name="target">Target entity we wish to gib</param>
    /// <param name="gibType">What type of gibing are we performing</param>
    /// <param name="gibContentsOption">What type of gibing do we perform on any container contents?</param>
    /// <param name="droppedEntities">a hashset containing all the entities that have been dropped/created</param>
    /// <param name="gibable">The gibable component</param>
    /// <param name="randomSpreadMod">How much to multiply the random spread on dropped giblets(if we are dropping them!)</param>
    /// <param name="playAudio">Should we play audio</param>
    /// <param name="allowedContainers">A list of containerIds on the target that permit gibing</param>
    /// <param name="excludedContainers">A list of containerIds on the target that DO NOT permit gibing</param>
    /// <param name="launchCone">The cone we are launching giblets in (if we are launching them!)</param>
    /// <param name="launchGibs">Should we launch giblets or just drop them</param>
    /// <param name="launchDirection">The direction to launch giblets (if we are launching them!)</param>
    /// <param name="launchImpulse">The impluse to launch giblets at(if we are launching them!)</param>
    /// <param name="launchImpulseVariance">The variation in giblet launch impulse (if we are launching them!)</param>
    /// <returns>True if successful, false if not</returns>
    public bool TryGibEntity(EntityUid outerEntity, EntityUid target, GibType gibType, GibContentsOption gibContentsOption,
        out HashSet<EntityUid> droppedEntities, GibbableComponent? gibable = null, bool launchGibs = true,
        Vector2 launchDirection = default, float launchImpulse = 0f, float launchImpulseVariance = 0f, Angle launchCone = default,
        float randomSpreadMod = 1.0f, bool playAudio = true, List<string>? allowedContainers = null, List<string>? excludedContainers = null)
    {
        droppedEntities = new();
        return TryGibEntityWithRef(outerEntity, target, gibType, gibContentsOption, ref droppedEntities, gibable,
            launchGibs, launchDirection, launchImpulse, launchImpulseVariance, launchCone, randomSpreadMod, playAudio,
            allowedContainers, excludedContainers);
    }


    /// <summary>
    /// Attempt to gib a specified entity. That entity must have a gibable components. This method is NOT recursive will only
    /// work on the target and any entities it contains (depending on gibContentsOption)
    /// </summary>
    /// <param name="outerEntity">The outermost entity we care about, used to place the dropped items</param>
    /// <param name="target">Target entity we wish to gib</param>
    /// <param name="gibType">What type of gibing are we performing</param>
    /// <param name="gibContentsOption">What type of gibing do we perform on any container contents?</param>
    /// <param name="droppedEntities">a hashset containing all the entities that have been dropped/created</param>
    /// <param name="gibable">The gibable component</param>
    /// <param name="randomSpreadMod">How much to multiply the random spread on dropped giblets(if we are dropping them!)</param>
    /// <param name="playAudio">Should we play audio</param>
    /// <param name="allowedContainers">A list of containerIds on the target that permit gibing</param>
    /// <param name="excludedContainers">A list of containerIds on the target that DO NOT permit gibing</param>
    /// <param name="launchCone">The cone we are launching giblets in (if we are launching them!)</param>
    /// <param name="launchGibs">Should we launch giblets or just drop them</param>
    /// <param name="launchDirection">The direction to launch giblets (if we are launching them!)</param>
    /// <param name="launchImpulse">The impluse to launch giblets at(if we are launching them!)</param>
    /// <param name="launchImpulseVariance">The variation in giblet launch impulse (if we are launching them!)</param>
    /// <returns>True if successful, false if not</returns>
    public bool TryGibEntityWithRef(EntityUid outerEntity, EntityUid target, GibType gibType, GibContentsOption gibContentsOption,
        ref HashSet<EntityUid> droppedEntities, GibbableComponent? gibable = null, bool launchGibs = true,
        Vector2? launchDirection = null, float launchImpulse = 0f, float launchImpulseVariance = 0f, Angle launchCone = default,
        float randomSpreadMod = 1.0f, bool playAudio = true, List<string>? allowedContainers = null, List<string>? excludedContainers = null)
    {
        if (!Resolve(target, ref gibable, logMissing: false))
            return false;
        if (gibType == GibType.Skip && gibContentsOption == GibContentsOption.Skip)
            return true;
        if (launchGibs)
        {
            randomSpreadMod = 0;
        }
        var parentXform = Transform(outerEntity);
        HashSet<BaseContainer> validContainers = new();
        foreach (var container in _containerSystem.GetAllContainers(target))
        {
            var valid = true;
            if (allowedContainers != null)
               valid = allowedContainers.Contains(container.ID);
            if (excludedContainers != null)
                    valid = valid && !excludedContainers.Contains(container.ID);
            if (valid)
                validContainers.Add(container);
        }
        switch (gibContentsOption)
        {
                case GibContentsOption.Skip:
                    break;
                case GibContentsOption.Drop:
                {
                    foreach (var container in validContainers)
                    {
                        foreach (var ent in container.ContainedEntities)
                        {
                            DropEntity(ent, parentXform, randomSpreadMod, gibable, ref droppedEntities, launchGibs,
                                launchDirection, launchImpulse, launchImpulseVariance, launchCone);
                        }
                    }
                    break;
                }
                case GibContentsOption.Gib:
                {
                    foreach (var container in _containerSystem.GetAllContainers(target))
                    {
                        foreach (var ent in container.ContainedEntities)
                        {
                            GibEntity(ent, parentXform, randomSpreadMod, gibable,ref droppedEntities, launchGibs,
                                launchDirection, launchImpulse, launchImpulseVariance, launchCone);
                        }
                    }
                    break;
                }
        }

        switch (gibType)
        {
            case GibType.Skip:
                break;
            case GibType.Drop:
            {
                DropEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities, launchGibs,
                    launchDirection, launchImpulse, launchImpulseVariance, launchCone);
                break;
            }
            case GibType.Gib:
            {
                GibEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities, launchGibs,
                    launchDirection, launchImpulse, launchImpulseVariance, launchCone);
                break;
            }
        }
        if (playAudio)
            _audioSystem.PlayPredicted(gibable.GibSound, parentXform.Coordinates, null, GibbableComponent.GibAudioParams);

        if (gibType == GibType.Gib)
            EntityManager.QueueDeleteEntity(target);
        return true;
    }

    private void DropEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod, GibbableComponent? gibable,
        ref HashSet<EntityUid> droppedEntities, bool flingEntity, Vector2? scatterDirection, float scatterImpulse,
        float scatterImpulseVariance, Angle scatterCone)
    {
        if (!Resolve(target, ref gibable, logMissing: false))
            return;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibCount, GibType.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return;
            case GibType.Gib:
                GibEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities, flingEntity,scatterDirection,
                    scatterImpulse, scatterImpulseVariance, scatterCone ,deleteTarget: false);
                return;
        }
        _transformSystem.AttachToGridOrMap(target);
        _transformSystem.SetCoordinates(target,parentXform.Coordinates);
        _transformSystem.SetWorldRotation(target, _random.NextAngle());
        droppedEntities.Add(target);
        if (flingEntity)
        {
            FlingDroppedEntity(target, scatterDirection, scatterImpulse, scatterImpulseVariance, scatterCone);
        }
        var gibbedEvent = new EntityGibbedEvent(target, new List<EntityUid>{target});
        RaiseLocalEvent(target,ref gibbedEvent);
    }

    private List<EntityUid> GibEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod,
        GibbableComponent? gibable, ref HashSet<EntityUid> droppedEntities, bool flingEntity, Vector2? scatterDirection, float scatterImpulse,
        float scatterImpulseVariance, Angle scatterCone, bool deleteTarget = true)
    {
        var localGibs = new List<EntityUid>();
        if (!Resolve(target, ref gibable, logMissing: false))
            return localGibs;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibCount, GibType.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return localGibs;
            case GibType.Drop:
                DropEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities, flingEntity,
                    scatterDirection, scatterImpulse, scatterImpulseVariance, scatterCone);
                localGibs.Add(target);
                return localGibs;
        }

        if (gibable.GibPrototypes.Count > 0)
        {
            if (flingEntity)
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (!TryCreateRandomGiblet(gibable, parentXform.Coordinates, false, out var giblet,
                            randomSpreadMod))
                        continue;
                    FlingDroppedEntity(giblet.Value, scatterDirection, scatterImpulse, scatterImpulseVariance,
                        scatterCone);
                    droppedEntities.Add(giblet.Value);

                }
            }
            else
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (TryCreateRandomGiblet(gibable, parentXform.Coordinates, false, out var giblet, randomSpreadMod))
                        droppedEntities.Add(giblet.Value);
                }
            }

        }
        _transformSystem.AttachToGridOrMap(target, Transform(target));
        if (flingEntity)
        {
            FlingDroppedEntity(target, scatterDirection, scatterImpulse, scatterImpulseVariance, scatterCone);
        }
        var gibbedEvent = new EntityGibbedEvent(target, localGibs);
        RaiseLocalEvent(target,ref gibbedEvent);
        if (deleteTarget)
            EntityManager.QueueDeleteEntity(target);
        return localGibs;
    }


    public bool TryCreateRandomGiblet(EntityUid target, [NotNullWhen(true)] out EntityUid? gibletEntity ,
        GibbableComponent? gibable = null, float randomSpreadModifier = 1.0f, bool playSound = true)
    {
        gibletEntity = null;
        return Resolve(target, ref gibable) && TryCreateRandomGiblet(gibable, Transform(target).Coordinates,
            playSound ,out gibletEntity, randomSpreadModifier);
    }

    public bool TryCreateAndFlingRandomGiblet(EntityUid target, [NotNullWhen(true)] out EntityUid? gibletEntity ,
        Vector2 scatterDirection, float force, float scatterImpulseVariance, Angle scatterCone = default, GibbableComponent? gibable = null, bool playSound = true)
    {
        gibletEntity = null;
        if (!Resolve(target, ref gibable) ||
            !TryCreateRandomGiblet(gibable, Transform(target).Coordinates, playSound, out gibletEntity))
            return false;
        FlingDroppedEntity(gibletEntity.Value, scatterDirection, force, scatterImpulseVariance, scatterCone);
        return true;
    }

    private void FlingDroppedEntity(EntityUid target, Vector2? direction, float impulse, float impulseVariance,
        Angle scatterConeAngle)
    {
        var scatterAngle = direction?.ToAngle() ?? _random.NextAngle();
        var scatterVector = _random.NextAngle(scatterAngle - scatterConeAngle/2,scatterAngle + scatterConeAngle/2)
            .ToVec()*(impulse+_random.NextFloat(impulseVariance));
        _physicsSystem.ApplyLinearImpulse(target, scatterVector);
    }

    private bool TryCreateRandomGiblet(GibbableComponent gibable, EntityCoordinates coords,
        bool playSound, [NotNullWhen(true)] out EntityUid? gibletEntity, float? randomSpreadModifier = null)
    {
        gibletEntity = null;
        if (gibable.GibPrototypes.Count == 0)
            return false;
        gibletEntity = EntityManager.SpawnEntity(gibable.GibPrototypes[_random.Next(0, gibable.GibPrototypes.Count)],
            randomSpreadModifier == null ? coords : coords.Offset(_random.NextVector2(GibbableComponent.GibScatterRange * randomSpreadModifier.Value)));
        if (playSound)
            _audioSystem.PlayPredicted(gibable.GibSound, coords, null, GibbableComponent.GibAudioParams);
        _transformSystem.SetWorldRotation(gibletEntity.Value, _random.NextAngle());
        return true;
    }
}
