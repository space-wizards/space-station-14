using System.Diagnostics.CodeAnalysis;
using Content.Shared.Gibing.Components;
using Content.Shared.Gibing.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Gibing.Systems;

public sealed class GibingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float GibScatterRange = 0.3f;
    private static readonly AudioParams GibAudioParams = AudioParams.Default.WithVariation(0.025f);

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
    public bool TryGibEntity(EntityUid outerEntity, EntityUid target, GibOption gibSettings, GibContentsOption gibContentsOption,
        out HashSet<EntityUid> droppedEntities, GibableComponent? gibable = null, float randomSpreadMod = 1.0f, bool playAudio = true,
        List<string>? containerWhitelist = null, List<string>? containerBlacklist = null)
    {
        droppedEntities = new();
        if (!Resolve(target, ref gibable))
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
                            var gibbedEvent = new EntityGibedEvent(target, new List<EntityUid>{ent});
                            RaiseLocalEvent(target,ref gibbedEvent);
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
                            var gibbedEvent = new EntityGibedEvent(ent, GibEntity(ent, parentXform,
                                randomSpreadMod, gibable,ref droppedEntities));
                            RaiseLocalEvent(target,ref gibbedEvent);
                            EntityManager.DeleteEntity(ent);
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
                var gibbedEvent = new EntityGibedEvent(target, new List<EntityUid>{target});
                RaiseLocalEvent(target,ref gibbedEvent);
                break;
            }
            case GibOption.Gib:
            {
                var gibbedEvent = new EntityGibedEvent(target, GibEntity(target, parentXform, randomSpreadMod, gibable, ref droppedEntities));
                RaiseLocalEvent(target,ref gibbedEvent);
                break;
            }
        }

        if (playAudio)
            _audioSystem.PlayPredicted(gibable.GibSound, parentXform.Coordinates, null, GibAudioParams);
        if (gibSettings == GibOption.Gib)
            EntityManager.DeleteEntity(target);
        return true;
    }

    private void DropEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod, GibableComponent? gibable,
        ref HashSet<EntityUid> droppedEntities)
    {
        if (!Resolve(target, ref gibable))
            return;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibletCount, GibOption.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        if (gibAttemptEvent.Canceled)
            return;
        _transformSystem.AttachToGridOrMap(target);
        _transformSystem.SetCoordinates(target,parentXform.Coordinates.Offset(_random.NextVector2(GibScatterRange * randomSpreadMod)));
        droppedEntities.Add(target);
    }

    private List<EntityUid> GibEntity(EntityUid target, TransformComponent parentXform, float randomSpreadMod,
        GibableComponent? gibable, ref HashSet<EntityUid> droppedEntities)
    {
        var localGibs = new List<EntityUid>();
        if (!Resolve(target, ref gibable))
            return localGibs;
        var gibAttemptEvent = new AttemptEntityGibEvent(target, gibable.GibletCount, GibOption.Drop);
        RaiseLocalEvent(target, ref gibAttemptEvent);
        if (gibAttemptEvent.Canceled)
            return localGibs;
        for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
        {
            if (TryCreateRandomGiblet(target, gibable, parentXform.Coordinates, randomSpreadMod, false, out var giblet))
                droppedEntities.Add(giblet.Value);
        }
        _transformSystem.AttachToGridOrMap(target, Transform(target));
        return localGibs;
    }


    public bool TryCreateRandomGiblet(EntityUid target, [NotNullWhen(true)] out EntityUid? gibletEntity ,
        GibableComponent? gibable = null, float randomSpreadModifier = 1.0f, bool playSound = true)
    {
        gibletEntity = null;
        return Resolve(target, ref gibable) && TryCreateRandomGiblet(target, gibable, Transform(target).Coordinates,
            randomSpreadModifier, playSound ,out gibletEntity);
    }


    private bool TryCreateRandomGiblet(EntityUid target, GibableComponent gibable, EntityCoordinates coords,
        float randomSpreadModifier, bool playSound, [NotNullWhen(true)] out EntityUid? gibletEntity)
    {
        gibletEntity = null;
        if (gibable.GibletPrototypes.Count == 0)
            return false;
        gibletEntity = EntityManager.SpawnEntity(gibable.GibletPrototypes[_random.Next(0, gibable.GibletPrototypes.Count)],
                coords.Offset(_random.NextVector2(GibScatterRange * randomSpreadModifier)));
        if (playSound)
            _audioSystem.PlayPredicted(gibable.GibSound, coords, null, GibAudioParams);
        return true;
    }
}
