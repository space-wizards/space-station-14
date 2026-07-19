using System.Linq;
using System.Numerics;
using Content.Shared.EntityTable;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Containers;

public sealed partial class ContainerFillSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerFillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EntityTableContainerFillComponent, MapInitEvent>(OnTableMapInit);
    }

    private void OnMapInit(EntityUid uid, ContainerFillComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out ContainerManagerComponent? containerComp))
            return;

        var xform = Transform(uid);

        if (!_transform.TryGetMapOrGridCoordinates(uid, out var coords, xform))
            return;

        foreach (var (contaienrId, prototypes) in component.Containers)
        {
            if (!_containerSystem.TryGetContainer(uid, contaienrId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} is missing a container ({contaienrId}).");
                continue;
            }

            foreach (var proto in prototypes)
            {
                var ent = Spawn(proto, coords.Value);
                if (!_containerSystem.Insert(ent, container, containerXform: xform))
                {
                    var alreadyContained = container.ContainedEntities.Count > 0 ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}")) : "< empty >";
                    Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} failed to insert an entity: {ToPrettyString(ent)}.\nCurrent contents:\n{alreadyContained}");
                    _transform.AttachToGridOrMap(ent);
                    break;
                }
            }
        }
    }

    private void OnTableMapInit(Entity<EntityTableContainerFillComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out ContainerManagerComponent? containerComp))
            return;

        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (containerId, table) in ent.Comp.Containers)
        {
            if (!_containerSystem.TryGetContainer(ent, containerId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(EntityTableContainerFillComponent)} is missing a container ({containerId}).");
                continue;
            }

            var spawns = _entityTable.GetSpawns(table).ToList();

            if (ent.Comp.Sort)
            {
                // Reverse order since we want to insert larger items first, and the list is sorted smallest to largest.
                spawns.Sort((a, b) => _itemSys.CompareSize(b, a));
            }

            foreach (var proto in spawns)
            {
                var spawn = Spawn(proto, coords);
                if (!_containerSystem.Insert(spawn, container, containerXform: xform))
                {
                    var alreadyContained = container.ContainedEntities.Count > 0 ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}")) : "< empty >";
                    Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(EntityTableContainerFillComponent)} failed to insert an entity: {ToPrettyString(spawn)}.\nCurrent contents:\n{alreadyContained}");
                    _transform.AttachToGridOrMap(spawn);
                    break;
                }
            }
        }
    }
}
