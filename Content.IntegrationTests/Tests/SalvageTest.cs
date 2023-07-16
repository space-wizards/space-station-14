using System.Threading.Tasks;
using Content.Server.Salvage;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class SalvageTest
    {
        [Test]
        public async Task SalvageGridBoundsTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            await server.WaitIdleAsync();

            var mapMan = server.ResolveDependency<IMapManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapLoader = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<MapLoaderSystem>();

            await server.WaitAssertion(() =>
            {
                foreach (var salvage in protoManager.EnumeratePrototypes<SalvageMapPrototype>())
                {
                    var mapId = mapMan.CreateMap();
                    mapLoader.TryLoad(mapId, salvage.MapPath.ToString(), out var rootUids);
                    Assert.That(rootUids is { Count: 1 }, $"Salvage map {salvage.ID} does not have a single grid");
                    var grid = rootUids[0];
                    Assert.That(entManager.TryGetComponent<MapGridComponent>(grid, out var gridComp), $"Salvage {salvage.ID}'s grid does not have GridComponent.");
                    Assert.That(gridComp.LocalAABB, Is.EqualTo(salvage.Bounds), $"Salvage {salvage.ID}'s bounds {gridComp.LocalAABB} are not equal to the bounds on the prototype {salvage.Bounds}");
                }
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
