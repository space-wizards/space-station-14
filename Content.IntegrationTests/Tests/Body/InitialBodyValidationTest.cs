using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
[TestOf(typeof(InitialBodySystem))]
public sealed class InitialBodyValidationTest : GameTest
{
    [SidedDependency(Side.Server)] private IComponentFactory _componentFactory = default!;
    [SidedDependency(Side.Server)] private IPrototypeManager _prototype = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void RelatedOrgansCanBeRelated()
    {
        using var scope = Assert.EnterMultipleScope();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || Pair.IsTestEntityPrototype(proto.ID))
                continue;

            if (!proto.TryComp<InitialBodyComponent>(out var initial, _componentFactory))
                continue;

            if (initial.Relationships is not { } relationships)
                continue;

            var organs = initial.Organs;
            foreach (var (parent, children) in relationships)
            {
                if (!organs.TryGetValue(parent, out var parentProtoId))
                    continue;

                var parentProto = _prototype.Index(parentProtoId);
                if (!parentProto.HasComp<ParentOrganComponent>(_componentFactory))
                {
                    Assert.Fail($"{proto.ID}'s parent organ {parent} {parentProtoId} is missing {nameof(ParentOrganComponent)}");
                }

                foreach (var child in children)
                {
                    if (!organs.TryGetValue(child, out var childProtoId))
                        continue;

                    var childProto = _prototype.Index(childProtoId);
                    if (!childProto.HasComp<ChildOrganComponent>(_componentFactory))
                    {
                        Assert.Fail($"{proto.ID}'s child organ {child} {childProtoId} (child of {parent} {parentProtoId}) is missing {nameof(ChildOrganComponent)}");
                    }
                }
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void NoMultipleParents()
    {
        using var scope = Assert.EnterMultipleScope();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || Pair.IsTestEntityPrototype(proto.ID))
                continue;

            if (!proto.TryComp<InitialBodyComponent>(out var initial, _componentFactory))
                continue;

            if (initial.Relationships is not { } relationships)
                continue;

            var parentOf = new Dictionary<ProtoId<OrganCategoryPrototype>, ProtoId<OrganCategoryPrototype>>();
            foreach (var (parent, children) in relationships)
            {
                foreach (var child in children)
                {
                    if (parentOf.TryGetValue(child, out var existingParent))
                    {
                        Assert.Fail($"{proto.ID}'s organ category {child} is claimed as a child by both {existingParent} and {parent}");
                    }

                    parentOf[child] = parent;
                }
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void InternalChildOrgansAreNotDetachable()
    {
        using var scope = Assert.EnterMultipleScope();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || Pair.IsTestEntityPrototype(proto.ID))
                continue;

            if (!proto.HasComp<InternalChildOrganComponent>(_componentFactory))
                continue;

            if (proto.HasComp<DetachableOrganComponent>(_componentFactory))
            {
                Assert.Fail($"{proto.ID} has both {nameof(InternalChildOrganComponent)} and {nameof(DetachableOrganComponent)}. Pick a lane, make your organs internal or detachable, but not both.");
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void NoCyclicalRelationships()
    {
        using var scope = Assert.EnterMultipleScope();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || Pair.IsTestEntityPrototype(proto.ID))
                continue;

            if (!proto.TryComp<InitialBodyComponent>(out var initial, _componentFactory))
                continue;

            if (initial.Relationships is not { } relationships)
                continue;

            var visited = new HashSet<ProtoId<OrganCategoryPrototype>>();
            var stack = new List<ProtoId<OrganCategoryPrototype>>();

            foreach (var parent in relationships.Keys)
            {
                if (TryFindCycle(parent, relationships, visited, stack, out var cycle))
                {
                    Assert.Fail($"{proto.ID} has a cycle in its InitialBodyComponent relationships: {string.Join(" -> ", cycle)}");
                }
            }
        }
    }

    private static bool TryFindCycle(
        ProtoId<OrganCategoryPrototype> node,
        Dictionary<ProtoId<OrganCategoryPrototype>, HashSet<ProtoId<OrganCategoryPrototype>>> relationships,
        HashSet<ProtoId<OrganCategoryPrototype>> visited,
        List<ProtoId<OrganCategoryPrototype>> stack,
        out List<ProtoId<OrganCategoryPrototype>> cycle)
    {
        if (stack.Contains(node))
        {
            var start = stack.IndexOf(node);
            cycle = stack.Skip(start).Append(node).ToList();
            return true;
        }

        cycle = new List<ProtoId<OrganCategoryPrototype>>();

        if (!visited.Add(node))
            return false;

        if (!relationships.TryGetValue(node, out var children))
            return false;

        stack.Add(node);

        foreach (var child in children)
        {
            if (TryFindCycle(child, relationships, visited, stack, out cycle))
                return true;
        }

        stack.RemoveAt(stack.Count - 1);
        return false;
    }
}
