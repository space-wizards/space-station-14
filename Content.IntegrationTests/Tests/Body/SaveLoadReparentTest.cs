using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Body;

[TestFixture]
public sealed class SaveLoadReparentTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: HumanBodyDummy
  id: HumanBodyDummy
  components:
  - type: Body
    prototype: Human
";

    [Test]
    public async Task Test()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entities = server.ResolveDependency<IEntityManager>();
        var maps = server.ResolveDependency<IMapManager>();
        var mapLoader = entities.System<MapLoaderSystem>();
        var bodySystem = entities.System<SharedBodySystem>();
        var containerSystem = entities.System<SharedContainerSystem>();
        var mapSys = entities.System<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var mapId);
            maps.CreateGrid(mapId);
            var human = entities.SpawnEntity("HumanBodyDummy", new MapCoordinates(0, 0, mapId));

            Assert.That(entities.HasComponent<BodyComponent>(human), Is.True);

            var parts = bodySystem.GetBodyChildren(human).Skip(1).ToArray();
            var organs = bodySystem.GetBodyOrgans(human).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(parts, Is.Not.Empty);
                Assert.That(organs, Is.Not.Empty);
            });

            foreach (var (id, component) in parts)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(component.Body, Is.EqualTo(human));
                    Assert.That(component.Body, Is.Not.Null);
                    var parent = bodySystem.GetParentPartOrNull(id);
                    Assert.That(parent, Is.Not.EqualTo(default(EntityUid)));
                    if (!bodySystem.IsPartRoot(component.Body.Value, id, null, component))
                    {
                        Assert.That(parent, Is.Not.Null);
                    }
                    else
                    {
                        Assert.That(parent, Is.Null);
                    }
                });

                foreach (var (slotId, slot) in component.Children)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(slot.Id, Is.EqualTo(slotId));
                        var container =
                            containerSystem.GetContainer(id, SharedBodySystem.GetPartSlotContainerId(slotId));
                        Assert.That(container.ContainedEntities, Is.Not.Empty);
                    });
                }
            }

            foreach (var (id, component) in organs)
            {
                var parent = bodySystem.GetParentPartOrNull(id);

                Assert.Multiple(() =>
                {
                    Assert.That(component.Body, Is.EqualTo(human));
                    Assert.That(parent, Is.Not.Null);
                    Assert.That(parent.Value, Is.Not.EqualTo(default(EntityUid)));
                });
            }

            // Converts an entity query enumerator to an enumerable.
            static IEnumerable<(EntityUid Uid, TComp Comp)> EnumerateQueryEnumerator<TComp>(EntityQueryEnumerator<TComp> query)
                where TComp : Component
            {
                while (query.MoveNext(out var uid, out var comp))
                    yield return (uid, comp);
            }

            Assert.That(
                EnumerateQueryEnumerator(
                    entities.EntityQueryEnumerator<BodyComponent>()
                ).Where((e) =>
                    entities.GetComponent<MetaDataComponent>(e.Uid).EntityPrototype!.Name == "HumanBodyDummy"
                ),
                Is.Not.Empty
            );

            const string mapPath = $"/{nameof(SaveLoadReparentTest)}{nameof(Test)}map.yml";

            mapLoader.SaveMap(mapId, mapPath);
            maps.DeleteMap(mapId);

            mapSys.CreateMap(out mapId);
            Assert.That(mapLoader.TryLoad(mapId, mapPath, out _), Is.True);

            var query = EnumerateQueryEnumerator(
                    entities.EntityQueryEnumerator<BodyComponent>()
                ).Where((e) =>
                    entities.GetComponent<MetaDataComponent>(e.Uid).EntityPrototype!.Name == "HumanBodyDummy"
                ).ToArray();

            Assert.That(query, Is.Not.Empty);
            foreach (var (uid, body) in query)
            {
                human = uid;
                parts = bodySystem.GetBodyChildren(human).Skip(1).ToArray();
                organs = bodySystem.GetBodyOrgans(human).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(parts, Is.Not.Empty);
                    Assert.That(organs, Is.Not.Empty);
                });

                foreach (var (id, component) in parts)
                {
                    var parent = bodySystem.GetParentPartOrNull(id);

                    Assert.Multiple(() =>
                    {
                        Assert.That(component.Body, Is.EqualTo(human));
                        Assert.That(parent, Is.Not.Null);
                        Assert.That(parent.Value, Is.Not.EqualTo(default(EntityUid)));
                    });

                    foreach (var (slotId, slot) in component.Children)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(slot.Id, Is.EqualTo(slotId));
                            var container =
                                containerSystem.GetContainer(id, SharedBodySystem.GetPartSlotContainerId(slotId));
                            Assert.That(container.ContainedEntities, Is.Not.Empty);
                        });
                    }
                }

                foreach (var (id, component) in organs)
                {
                    var parent = bodySystem.GetParentPartOrNull(id);

                    Assert.Multiple(() =>
                    {
                        Assert.That(component.Body, Is.EqualTo(human));
                        Assert.That(parent, Is.Not.Null);
                        Assert.That(parent.Value, Is.Not.EqualTo(default(EntityUid)));
                    });
                }

                maps.DeleteMap(mapId);
            }
        });

        await pair.CleanReturnAsync();
    }
}
