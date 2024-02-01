using System.Diagnostics.CodeAnalysis;
using Content.Shared.Gibing.Components;
using Content.Shared.Gibing.Events;
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

    //TODO: (future optimization) implement a system that "caps" giblet entities by deleting the oldest ones once we reach a certain limit, customizable via CVAR


    public bool GibEntity(EntityUid targetEnt, out List<EntityUid>? droppedEntities, out List<EntityUid>? giblets,
        GibableComponent? gibable = null, bool spawnGibblets = true,
        bool gibContained = false, float randomSpreadModifier = 1.0f, bool playSound = true)
    {
        droppedEntities = new List<EntityUid>();
        giblets = new();
        var parentXform = Transform(targetEnt);
        if (!Resolve(targetEnt, ref gibable))
            return false;


        TryComp<ContainerManagerComponent>(parentXform.ParentUid, out var conMan);
        if (!GibEntityRecursive(parentXform.ParentUid, parentXform, conMan, targetEnt, gibable, ref droppedEntities,
                ref giblets, spawnGibblets, gibContained, randomSpreadModifier))
            return false;

        //only play gibbing sound once instead of per gib!
        if (playSound)
            _audioSystem.PlayPvs(gibable.GibSound, parentXform.Coordinates);
        return true;
    }
    private bool GibEntityRecursive(EntityUid parentEnt,TransformComponent parentXform, ContainerManagerComponent? conMan, EntityUid targetEnt,
        GibableComponent? gibable, ref List<EntityUid> droppedEntities,
        ref List<EntityUid> giblets, bool spawnGibblets,
        bool gibContained, float randomSpreadModifier)
    {
        if (!Resolve(targetEnt, ref gibable))
            return false;
        var preGibEvent = new PreEntityGibedEvent(targetEnt, gibable.GibletCount);
        RaiseLocalEvent(targetEnt, ref preGibEvent);
        if (preGibEvent.Canceled)
            return false;

        List<EntityUid> localDropped = new();
        if (!gibContained)
        {
            foreach (var container in _containerSystem.GetAllContainers(parentEnt, conMan))
            {
                foreach (var ent in container.ContainedEntities)
                {
                    _transformSystem.AttachToGridOrMap(ent, parentXform);
                    droppedEntities.Add(ent);
                    localDropped.Add(ent);
                    _transformSystem.SetCoordinates(ent, parentXform.Coordinates.Offset(_random.NextVector2(GibScatterRange * randomSpreadModifier)));
                }
            }
        }
        else
        {
            foreach (var container in _containerSystem.GetAllContainers(parentEnt, conMan))
            {
                foreach (var ent in container.ContainedEntities)
                {
                    if (GibEntityRecursive(targetEnt, parentXform, conMan, ent, null, ref droppedEntities,
                            ref giblets, spawnGibblets, gibContained, randomSpreadModifier))
                        continue;
                    _transformSystem.AttachToGridOrMap(ent, parentXform);
                    droppedEntities.Add(ent);
                    localDropped.Add(ent);
                    _transformSystem.SetCoordinates(ent, parentXform.Coordinates.Offset(_random.NextVector2(GibScatterRange * randomSpreadModifier)));
                }
            }
        }

        List<EntityUid> localGibs = new();
        if (spawnGibblets)
        {
            for (int i = 0; i < gibable.GibletCount; i++)
            {
                if (TryCreateRandomGiblet(targetEnt, gibable, parentXform.Coordinates, randomSpreadModifier, false, out var newGiblet))
                {
                    giblets.Add(newGiblet.Value);
                    localGibs.Add(newGiblet.Value);
                }

            }
        }
        var entGibbedEvent = new EntityGibedEvent(targetEnt, localGibs, localDropped);
        RaiseLocalEvent(targetEnt, ref entGibbedEvent);
        EntityManager.DeleteEntity(targetEnt);
        return true;
    }

    public bool TryCreateRandomGiblet(EntityUid target, [NotNullWhen(true)] out EntityUid? gibletEntity ,
        GibableComponent? gibable = null, float randomSpreadModifier = 1.0f, bool playSound = true)
    {
        gibletEntity = null;
        return Resolve(target, ref gibable) && TryCreateRandomGiblet(target, gibable, Transform(target).Coordinates, randomSpreadModifier, playSound ,out gibletEntity);
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
            _audioSystem.PlayPvs(gibable.GibSound, coords);
        return true;
    }


}
