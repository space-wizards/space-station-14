using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Shared.Body;

namespace Content.IntegrationTests.Tests.Body;

[TestOf(typeof(OrganRelationSystem))]
public sealed class OrganRelationSystemTest : GameTest
{
    private const string OrganRelationTestOrgan = "OrganRelationTestOrgan";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {OrganRelationTestOrgan}
  components:
  - type: Organ
  - type: ParentOrgan
  - type: ChildOrgan
";

    [SidedDependency(Side.Server)] private OrganRelationSystem _organRelation = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void RelateAndOrphan()
    {
        var parent = SSpawn(OrganRelationTestOrgan);
        var child = SSpawn(OrganRelationTestOrgan);

        _organRelation.Relate(parent, child);

        var parentComp = SComp<ParentOrganComponent>(parent);
        var childComp = SComp<ChildOrganComponent>(child);

        Assert.That(parentComp.Children, Does.Contain(child));
        Assert.That(childComp.Parent, Is.EqualTo(parent));

        _organRelation.Orphan(child);

        Assert.That(parentComp.Children, Does.Not.Contain(child));
        Assert.That(childComp.Parent, Is.Null);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void Traversal()
    {
        var grandParent = SSpawn(OrganRelationTestOrgan);
        var parent = SSpawn(OrganRelationTestOrgan);
        var child = SSpawn(OrganRelationTestOrgan);

        _organRelation.Relate(grandParent, parent);
        _organRelation.Relate(parent, child);

        var allChildren = _organRelation.AllChildren(grandParent).Select(e => e.Owner).ToList();
        Assert.That(allChildren, Is.EquivalentTo([parent, child]));

        var allParents = _organRelation.AllParents(child).Select(e => e.Owner).ToList();
        Assert.That(allParents, Is.EquivalentTo([parent, grandParent]));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ParentDeletion()
    {
        var parent = SSpawn(OrganRelationTestOrgan);
        var child = SSpawn(OrganRelationTestOrgan);

        _organRelation.Relate(parent, child);

        SDeleteNow(parent);

        Assert.That(child, Is.Not.Deleted(Server));
        Assert.That(SComp<ChildOrganComponent>(child).Parent, Is.Null);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ChildDeletion()
    {
        var parent = SSpawn(OrganRelationTestOrgan);
        var child = SSpawn(OrganRelationTestOrgan);

        _organRelation.Relate(parent, child);

        SDeleteNow(child);

        Assert.That(SComp<ParentOrganComponent>(parent).Children, Does.Not.Contain(child));
    }
}
