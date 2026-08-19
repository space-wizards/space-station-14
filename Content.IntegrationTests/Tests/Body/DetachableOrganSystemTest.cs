#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests.Body;

[TestOf(typeof(DetachableOrganSystem))]
public sealed class DetachableOrganSystemTest : GameTest
{
    private const string DetachTestOldBody = "DetachTestOldBody";
    private const string DetachTestNewBody = "DetachTestNewBody";
    private const string DetachTestGrandParentOrgan = "DetachTestGrandParentOrgan";
    private const string DetachTestRootOrgan = "DetachTestRootOrgan";
    private const string DetachTestChildOrgan = "DetachTestChildOrgan";
    private const string DetachTestSiblingOrgan = "DetachTestSiblingOrgan";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {DetachTestOldBody}
  components:
  - type: Body

- type: entity
  id: {DetachTestNewBody}
  components:
  - type: Body

- type: entity
  id: {DetachTestGrandParentOrgan}
  components:
  - type: Organ
  - type: ParentOrgan
  - type: ChildOrgan

- type: entity
  id: {DetachTestRootOrgan}
  components:
  - type: Organ
  - type: ParentOrgan
  - type: ChildOrgan
  - type: DetachableOrgan
    detachedBody: DetachTestNewBody

- type: entity
  id: {DetachTestChildOrgan}
  components:
  - type: Organ
  - type: ChildOrgan

- type: entity
  id: {DetachTestSiblingOrgan}
  components:
  - type: Organ
  - type: ChildOrgan
";

    [SidedDependency(Side.Server)] private SharedContainerSystem _container = default!;
    [SidedDependency(Side.Server)] private OrganRelationSystem _organRelation = default!;
    [SidedDependency(Side.Server)] private DetachableOrganSystem _detachableOrgan = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void Detach()
    {
        var oldBody = SSpawn(DetachTestOldBody);
        var oldBodyContainer = _container.GetContainer(oldBody, BodyComponent.ContainerID);

        var grandParent = SSpawn(DetachTestGrandParentOrgan);
        var root = SSpawn(DetachTestRootOrgan);
        var child = SSpawn(DetachTestChildOrgan);
        var sibling = SSpawn(DetachTestSiblingOrgan);

        _container.Insert(grandParent, oldBodyContainer, force: true);
        _container.Insert(root, oldBodyContainer, force: true);
        _container.Insert(child, oldBodyContainer, force: true);
        _container.Insert(sibling, oldBodyContainer, force: true);

        _organRelation.Relate(grandParent, root);
        _organRelation.Relate(grandParent, sibling);
        _organRelation.Relate(root, child);

        var newBody = _detachableOrgan.Detach(root);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(newBody, Is.Not.Null);

            var newBodyContained = _container.GetContainer(newBody!.Value, BodyComponent.ContainerID).ContainedEntities.ToList();
            Assert.That(newBodyContained, Is.EquivalentTo([root, child]));

            var oldBodyContained = oldBodyContainer.ContainedEntities.ToList();
            Assert.That(oldBodyContained, Is.EquivalentTo([grandParent, sibling]));

            var grandParentComp = SComp<ParentOrganComponent>(grandParent);
            Assert.That(grandParentComp.Children, Does.Not.Contain(root));
            Assert.That(grandParentComp.Children, Does.Contain(sibling));

            Assert.That(SComp<ChildOrganComponent>(root).Parent, Is.Null);
            Assert.That(SComp<ParentOrganComponent>(root).Children, Does.Contain(child));
            Assert.That(SComp<ChildOrganComponent>(child).Parent, Is.EqualTo(root));

            Assert.That(SComp<OrganComponent>(root).Body, Is.EqualTo(newBody));
            Assert.That(SComp<OrganComponent>(child).Body, Is.EqualTo(newBody));
            Assert.That(SComp<OrganComponent>(grandParent).Body, Is.EqualTo(oldBody));
            Assert.That(SComp<OrganComponent>(sibling).Body, Is.EqualTo(oldBody));
        }

    }
}
