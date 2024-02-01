using System.Diagnostics.CodeAnalysis;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Gibbing.Systems;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    //TODO: (future optimization) implement a system that "caps" giblet entities by deleting the oldest ones once we reach a certain limit, customizable via CVAR


    /// <summary>
    /// Attempt to gib a specified entity. That entity must have a gibable components. This method is NOT recursive will only
    /// work on the target and any entities it contains (depending on gibContentsOption)
    /// </summary>
    /// <param name="outerEntity">The outermost entity we care about, used to place the dropped items</param>
    /// <param name="target">Target entity we wish to gib</param>
    /// <param name="gibSettings">What type of gibing are we performing</param>
    /// <param name="gibContentsOption">What type of gibing do we perform on any container contents?</param>
    /// <param name="droppedEntities">a hashset containing all the entities that have been dropped/created</param>
    /// <param name="gibable">The gibable component</param>
    /// <param name="randomSpreadMod">How much to multiply the random spread on drops for</param>
    /// <param name="playAudio">Should we play audio</param>
    /// <param name="containerWhitelist">A list of containerIds on the target that permit gibing</param>
    /// <param name="containerBlacklist">A list of containerIds on the target that DO NOT permit gibing</param>
    /// <returns>True if successful, false if not</returns>
    public bool TryGibEntity(EntityUid outerEntity, EntityUid target, GibOption gibSettings,
        GibContentsOption gibContentsOption,
        out HashSet<EntityUid> droppedEntities, GibbableComponent? gibable = null, float randomSpreadMod = 1.0f,
        bool playAudio = true,
        List<string>? containerWhitelist = null, List<string>? containerBlacklist = null)
    {
        droppedEntities = new();
        return TryGibEntityWithRef(outerEntity, target, gibSettings, gibContentsOption, ref droppedEntities, gibable,
            randomSpreadMod, playAudio, containerWhitelist, containerBlacklist);
    }


    /// <summary>
    /// Attempt to gib a specified entity. That entity must have a gibable components. This method is NOT recursive will only
    /// work on the target and any entities it contains (depending on gibContentsOption)
    /// </summary>
    /// <param name="outerEntity">The outermost entity we care about, used to place the dropped items</param>
    /// <param name="target">Target entity we wish to gib</param>
    /// <param name="gibSettings">What type of gibing are we performing</param>
    /// <param name="gibContentsOption">What type of gibing do we perform on any container contents?</param>
    /// <param name="droppedEntities">a hashset containing all the entities that have been dropped/created</param>
    /// <param name="gibable">The gibable component</param>
    /// <param name="randomSpreadMod">How much to multiply the random spread on drops for</param>
    /// <param name="playAudio">Should we play audio</param>
    /// <param name="containerWhitelist">A list of containerIds on the target that permit gibing</param>
    /// <param name="containerBlacklist">A list of containerIds on the target that DO NOT permit gibing</param>
    /// <returns>True if successful, false if not</returns>
    public bool TryGibEntityWithRef(EntityUid outerEntity, EntityUid target, GibOption gibSettings, GibContentsOption gibContentsOption,
        ref HashSet<EntityUid> droppedEntities, GibbableComponent? gibable = null, float randomSpreadMod = 1.0f, bool playAudio = true,
        List<string>? containerWhitelist = null, List<string>? containerBlacklist = null)
    {
        if (!Resolve(target, ref gibable, logMissing: false))
            return false;
        if (gibSettings == GibOption.Skip && gibContentsOption == GibContentsOption.Skip)
            return true;
        var parentXform = Transform(outerEntity);
        HashSet<BaseContainer> validContainers = new();
        foreach (var container in _containerSystem.GetAllContainers(target))
        {
            var valid = true;
            if (containerWhitelist != null)
               valid = containerWhitelist.Contains(container.ID);
            if (containerBlacklist != null)
                    valid = valid && !containerBlacklist.Contains(container.ID);
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
                            DropEntity(ent, parentXform, randomSpreadMod, gibable, ref droppedEntities);
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
                            GibEntity(ent, parentXform, randomSpreadMod, gibable,ref droppedEntities);
                        }
                    }
                    break;
                }
        }

        switch (gibSettings)
        {
            case GibOption.Skip:
                break;
            case GibOption.Drop:
            {
                DropEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities);
                break;
            }
            case GibOption.Gib:
            {
                GibEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities);
                break;
            }
        }

        if (playAudio)
            _audioSystem.PlayPredicted(gibable.GibSound, parentXform.Coordinates, null, GibbableComponent.GibAudioParams);
        if (gibSettings == GibOption.Gib)
            EntityManager.QueueDeleteEntity(target);
        return true;
    }

    private void DropEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod, GibbableComponent? gibable,
        ref HashSet<EntityUid> droppedEntities)
    {
        if (!Resolve(target, ref gibable, logMissing: false))
            return;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibCount, GibOption.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        if (gibAttemptEvent.GibOption == GibOption.Skip)
            return;
        if (gibAttemptEvent.GibOption == GibOption.Gib)
        {
            GibEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities, deleteTarget: false);
            return;
        }
        _transformSystem.AttachToGridOrMap(target);
        _transformSystem.SetCoordinates(target,parentXform.Coordinates.Offset(_random.NextVector2(GibbableComponent.GibScatterRange * randomSpreadMod)));
        droppedEntities.Add(target);
        var gibbedEvent = new EntityGibbedEvent(target, new List<EntityUid>{target});
        RaiseLocalEvent(target,ref gibbedEvent);
    }

    private List<EntityUid> GibEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod,
        GibbableComponent? gibable, ref HashSet<EntityUid> droppedEntities, bool deleteTarget = true)
    {
        var localGibs = new List<EntityUid>();
        if (!Resolve(target, ref gibable, logMissing: false))
            return localGibs;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibCount, GibOption.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        if (gibAttemptEvent.GibOption == GibOption.Skip)
            return localGibs;
        if (gibAttemptEvent.GibOption == GibOption.Drop)
        {
            DropEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities);
            localGibs.Add(target);
            return localGibs;
        }
        if (gibable.GibPrototypes.Count > 0)
        {
            for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
            {
                if (TryCreateRandomGiblet(target, gibable, parentXform.Coordinates, randomSpreadMod, false, out var giblet))
                    droppedEntities.Add(giblet.Value);
            }
        }
        _transformSystem.AttachToGridOrMap(target, Transform(target));
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
        return Resolve(target, ref gibable) && TryCreateRandomGiblet(target, gibable, Transform(target).Coordinates,
            randomSpreadModifier, playSound ,out gibletEntity);
    }


    private bool TryCreateRandomGiblet(EntityUid target, GibbableComponent gibable, EntityCoordinates coords,
        float randomSpreadModifier, bool playSound, [NotNullWhen(true)] out EntityUid? gibletEntity)
    {
        gibletEntity = null;
        if (gibable.GibPrototypes.Count == 0)
            return false;
        gibletEntity = EntityManager.SpawnEntity(gibable.GibPrototypes[_random.Next(0, gibable.GibPrototypes.Count)],
                coords.Offset(_random.NextVector2(GibbableComponent.GibScatterRange * randomSpreadModifier)));
        if (playSound)
            _audioSystem.PlayPredicted(gibable.GibSound, coords, null, GibbableComponent.GibAudioParams);
        return true;
    }
}
