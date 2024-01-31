using Content.Shared.FixedPoint;
using Content.Shared.Gibing.Components;
using Content.Shared.Gibing.Events;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.Gibing.Systems;

public sealed class GibingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    //TODO: (future optimization) implement a system that "caps" giblet entities by deleting the oldest ones once we reach a certain limit, customizable via CVAR

    public bool GibEntity(EntityUid targetEnt, GibableComponent? gibable = null, bool spawnGibblets = true ,
        bool dumpContainedEntities = false, float randomSpreadModifier = 1.0f, float force = 0f, bool playSound = true)
    {
        if (!Resolve(targetEnt, ref gibable))
            return false;
        var preGibEvent = new PreEntityGibedEvent(targetEnt, gibable.GibletCount);
        RaiseLocalEvent(targetEnt, preGibEvent);
        if (preGibEvent.Canceled)
            return false;
        List<EntityUid> containedEnts = new();
        if (dumpContainedEntities)
        {
            foreach (var container in _containerSystem.GetAllContainers(targetEnt))
            {
                containedEnts.AddRange(container.ContainedEntities);
                foreach (var ent in container.ContainedEntities)
                {
                    _transformSystem.AttachToGridOrMap(ent);
                    _transformSystem.SetCoordinates(ent, Transform(container.Owner).Coordinates.Offset(_random.NextVector2(.3f * randomSpreadModifier)));

                }
            }
        }


        return true;
    }

}
