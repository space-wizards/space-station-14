using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Body;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class OrganCommand : ToolshedCommand
{
    private DetachableOrganSystem? _detachableOrgan;
    private SharedContainerSystem? _container;
    private OrganRelationSystem? _organRelation;

    [CommandImplementation("parent")]
    public EntityUid? Parent([PipedArgument] EntityUid child)
    {
        if (!TryComp<ChildOrganComponent>(child, out var childOrgan))
            return null;

        return childOrgan.Parent;
    }

    [CommandImplementation("parent")]
    public IEnumerable<EntityUid?> Parent([PipedArgument] IEnumerable<EntityUid> children)
    {
        return children.Select(Parent);
    }

    [CommandImplementation("children")]
    public IEnumerable<EntityUid> Children([PipedArgument] EntityUid parent)
    {
        if (!TryComp<ParentOrganComponent>(parent, out var parentOrgan))
            yield break;

        foreach (var child in parentOrgan.Children)
        {
            yield return child;
        }
    }

    [CommandImplementation("children")]
    public IEnumerable<EntityUid> Children([PipedArgument] IEnumerable<EntityUid> parents)
    {
        return parents.SelectMany(Children);
    }

    [CommandImplementation("detach")]
    public EntityUid? Detach([PipedArgument] EntityUid organ)
    {
        _detachableOrgan ??= GetSys<DetachableOrganSystem>();
        return _detachableOrgan.Detach(organ);
    }

    [CommandImplementation("detach")]
    public IEnumerable<EntityUid?> Detach([PipedArgument] IEnumerable<EntityUid> organs)
    {
        return organs.Select(Detach);
    }

    [CommandImplementation("attach")]
    public void Attach([PipedArgument] EntityUid parent, EntityUid child)
    {
        _container ??= GetSys<SharedContainerSystem>();
        _organRelation ??= GetSys<OrganRelationSystem>();

        if (!TryComp<ParentOrganComponent>(parent, out var parentOrgan) ||
            !TryComp<ChildOrganComponent>(child, out var childOrgan) ||
            !TryComp<OrganComponent>(parent, out var parentOrganComponent) ||
            parentOrganComponent.Body is not { } body)
            return;

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var container))
            return;

        _container.Insert(child, container, force: true);
        _organRelation.Relate((parent, parentOrgan), (child, childOrgan));
    }

    [CommandImplementation("attach")]
    public void Attach([PipedArgument] IEnumerable<EntityUid> parents, EntityUid child)
    {
        foreach (var parent in parents)
        {
            Attach(parent, child);
        }
    }

    [CommandImplementation("is")]
    public bool Is([PipedArgument] EntityUid organ, ProtoId<OrganCategoryPrototype> category)
    {
        if (!TryComp<OrganComponent>(organ, out var organComp))
            return false;

        return organComp.Category == category;
    }

    [CommandImplementation("is")]
    public IEnumerable<bool> Is([PipedArgument] IEnumerable<EntityUid> organs, ProtoId<OrganCategoryPrototype> category)
    {
        return organs.Select(it => Is(it, category));
    }

    [CommandImplementation("of_type")]
    public IEnumerable<EntityUid> Typed([PipedArgument] IEnumerable<EntityUid> organs, ProtoId<OrganCategoryPrototype> category)
    {
        return organs.Where(it => Is(it, category));
    }
}
