#nullable enable
using NUnit.Framework;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


/// <summary>
/// Materials and stacks have some odd relationships to entities,
/// so we need some test coverage for them.
/// </summary>
namespace Content.IntegrationTests.Tests.Materials
{
    [TestFixture]
    [TestOf(typeof(StackSystem))]
    [TestOf(typeof(MaterialPrototype))]
    public sealed class MaterialPrototypeSpawnsStackMaterialTest
    {
        [Test]
        public async Task MaterialPrototypeSpawnsStackMaterial()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var allMaterialProtos = prototypeManager.EnumeratePrototypes<MaterialPrototype>();
                var coords = testMap.GridCoords;

                foreach (var proto in allMaterialProtos)
                {
                    if (proto.StackEntity == "")
                        continue;

                    var spawned = entityManager.SpawnEntity(proto.StackEntity, coords);

                    Assert.That(entityManager.HasComponent<StackComponent>(spawned),
                        $"{proto.ID} 'stack entity' {proto.StackEntity} has the stack component");

                    Assert.That(entityManager.HasComponent<MaterialComponent>(spawned),
                        $"{proto.ID} 'material stack' {proto.StackEntity} has the material component");
                }

                mapManager.DeleteMap(testMap.MapId);
            });
        }
    }
}
