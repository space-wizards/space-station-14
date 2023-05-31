using System.Threading.Tasks;
using Content.Tests;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Shuttle;

public sealed class DockTest : ContentUnitTest
{
    [Test]
    public async Task TestDockingConfig()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings() { NoClient = true });
        var server = pair.Pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var map = await PoolManager.CreateTestMap(pair);

        await server.WaitAssertion(() =>
        {
            var grid1 = mapManager.CreateGrid()
        });

        await pair.CleanReturnAsync();
    }
}
