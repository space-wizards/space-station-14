using System.Linq;
using System.Numerics;
using Content.Shared.EntityTable;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Containers;

public sealed class ContainerFillSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
        var coords = new EntityCoordinates(uid, Vector2.Zero);

        foreach (var (contaienrId, prototypes) in component.Containers)
        {
            if (!_containerSystem.TryGetContainer(uid, contaienrId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} is missing a container ({contaienrId}).");
                continue;
            }

            foreach (var proto in prototypes)
            {
                var ent = Spawn(proto, coords);
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

        // we include special behavior for storage due to stack merging and other things that it handles.
        var storageComp = CompOrNull<StorageComponent>(ent);

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (containerId, table) in ent.Comp.Containers)
        {
            if (!_containerSystem.TryGetContainer(ent, containerId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(EntityTableContainerFillComponent)} is missing a container ({containerId}).");
                continue;
            }

            var spawns = _entityTable.GetSpawns(table)
                .OrderByDescending(p => _prototype.Index(p).TryGetComponent<ItemComponent>(out var item, EntityManager.ComponentFactory)
                    ? _item.GetItemShape(item).GetArea()
                    : int.MaxValue);

            foreach (var proto in spawns)
            {
                var spawn = Spawn(proto, coords);

                var success = storageComp != null && containerId == StorageComponent.ContainerId
                    ? _storage.Insert(ent, spawn, out _, storageComp: storageComp, playSound: false)
                    : _containerSystem.Insert(spawn, container, containerXform: xform);

                if (!success)
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
