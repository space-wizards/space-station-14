using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Toolshed;

namespace Content.Server.Storage;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class StorageCommand : ToolshedCommand
{
    private SharedStorageSystem? _storage;
    private SharedContainerSystem? _container;


    [CommandImplementation("insert")]
    public IEnumerable<EntityUid> StorageInsert([PipedArgument] IEnumerable<EntityUid> entsToInsert,
        EntityUid targetEnt) => entsToInsert.Where(x => StorageInsert(x, targetEnt) != null);

    public EntityUid? StorageInsert(EntityUid entToInsert, EntityUid targetEnt)
    {
        _storage ??= GetSys<SharedStorageSystem>();

        if (!EntityManager.TryGetComponent<StorageComponent>(targetEnt, out var storage))
            return null;

        return _storage.Insert(targetEnt, entToInsert, out var stackedEntity, null, storage, false)
            ? entToInsert
            : null;
    }


    [CommandImplementation("fasttake")]
    public IEnumerable<EntityUid> StorageFastTake([PipedArgument] IEnumerable<EntityUid> storageEnts) =>
        storageEnts.Select(StorageFastTake).OfType<EntityUid>();

    public EntityUid? StorageFastTake(EntityUid storageEnt)
    {
        _storage ??= GetSys<SharedStorageSystem>();
        _container ??= GetSys<SharedContainerSystem>();


        if (!EntityManager.TryGetComponent<StorageComponent>(storageEnt, out var storage))
            return null;

        var removing = storage.Container.ContainedEntities[^1];
        if (_container.RemoveEntity(storageEnt, removing))
            return removing;

        return null;
    }



}
