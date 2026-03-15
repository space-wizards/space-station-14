using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Containers;
using Robust.Shared.Toolshed;

namespace Content.Server.Containers;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class ContainerCommand : ToolshedCommand
{
    private SharedContainerSystem? _container;

    [CommandImplementation("query")]
    public IEnumerable<EntityUid> ContainerQuery([PipedArgument] IEnumerable<EntityUid> storageEnts, string id) =>
        storageEnts.SelectMany(x => ContainerQueryBase(x, id));


    public IEnumerable<EntityUid> ContainerQueryBase(EntityUid ent, string id)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(ent, id, out var container))
            return [];

        return container.ContainedEntities;
    }

    [CommandImplementation("get")]
    public IEnumerable<BaseContainer> ContainerGet([PipedArgument] IEnumerable<EntityUid> storageEnts, string id) =>
        storageEnts.Select(x => ContainerGetBase(x, id)).Where(s => s != null).Select(s => s!);


    public BaseContainer? ContainerGetBase(EntityUid ent, string id)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(ent, id, out var container))
            return null;

        return container;
    }

    [CommandImplementation("insertmultiple")]
    public BaseContainer ContainerInsert([PipedArgument] BaseContainer container, bool doForce, IEnumerable<EntityUid> ents)
    {
        _container ??= GetSys<SharedContainerSystem>();

        foreach (var ent in ents)
        {
            if (doForce)
            {
                _container.Insert(ent, container, null, true);
            }
            else
            {
                _container.InsertOrDrop(ent, container);
            }
        }
        return container;
    }

    [CommandImplementation("insert")]
    public BaseContainer ContainerInsert([PipedArgument] BaseContainer container, bool doForce, EntityUid ent)
    {
        return ContainerInsert(container, doForce, [ent]);
    }

    [CommandImplementation("list")]
    public IEnumerable<string> ContainerList([PipedArgument] EntityUid ent)
    {
        _container ??= GetSys<SharedContainerSystem>();

        return _container.GetAllContainers(ent).Select(container => container.ID);
    }
    [CommandImplementation("getall")]
    public IEnumerable<BaseContainer> ContainerGetAll([PipedArgument] EntityUid ent)
    {
        _container ??= GetSys<SharedContainerSystem>();

        return _container.GetAllContainers(ent);
    }
}
