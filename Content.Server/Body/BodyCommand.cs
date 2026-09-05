using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Toolshed;

namespace Content.Server.Body;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class BodyCommand : ToolshedCommand
{
    private SharedContainerSystem? _container;

    [CommandImplementation("insert")]
    public void Insert([PipedArgument] EntityUid body, EntityUid organ)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return;

        _container.Insert(organ, container, force: true);
    }

    [CommandImplementation("organs")]
    public IEnumerable<EntityUid> Organs([PipedArgument] EntityUid body)
    {
        _container ??= GetSys<SharedContainerSystem>();

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return Enumerable.Empty<EntityUid>();

        return container.ContainedEntities.ToList();
    }

    [CommandImplementation("organs")]
    public IEnumerable<EntityUid> Organs([PipedArgument] IEnumerable<EntityUid> body)
    {
        return body.SelectMany(Organs);
    }
}
