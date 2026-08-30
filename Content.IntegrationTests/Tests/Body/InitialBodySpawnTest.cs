using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
[TestOf(typeof(InitialBodySystem))]
public sealed class InitialBodySpawnTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: organCategory
  id: InitialBodySpawnTestParent

- type: organCategory
  id: InitialBodySpawnTestChild

- type: entity
  id: InitialBodySpawnTestBody
  components:
  - type: Body
  - type: InitialBody
    organs:
      InitialBodySpawnTestParent: InitialBodySpawnTestParentOrgan
      InitialBodySpawnTestChild: InitialBodySpawnTestChildOrgan
    relationships:
      InitialBodySpawnTestParent: [InitialBodySpawnTestChild]

- type: entity
  id: InitialBodySpawnTestParentOrgan
  components:
  - type: Organ
  - type: ParentOrgan

- type: entity
  id: InitialBodySpawnTestChildOrgan
  components:
  - type: Organ
  - type: ChildOrgan
";

    [SidedDependency(Side.Server)] private SharedContainerSystem _container = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void SpawningWiresUpOrganRelations()
    {
        var body = SSpawn("InitialBodySpawnTestBody");

        var bodyContainer = _container.GetContainer(body, BodyComponent.ContainerID);
        var contained = bodyContainer.ContainedEntities.ToList();
        Assert.That(contained, Has.Count.EqualTo(2));

        var parent = contained.Single(SEntMan.HasComponent<ParentOrganComponent>);
        var child = contained.Single(SEntMan.HasComponent<ChildOrganComponent>);

        var parentComp = SComp<ParentOrganComponent>(parent);
        var childComp = SComp<ChildOrganComponent>(child);

        Assert.That(parentComp.Children, Does.Contain(child));
        Assert.That(childComp.Parent, Is.EqualTo(parent));
    }
}
