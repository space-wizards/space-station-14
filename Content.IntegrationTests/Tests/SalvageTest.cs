using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Content.Shared.Salvage;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class SalvageTest : GameTest
{
    /// <summary>
    /// Asserts that all salvage maps have been saved as grids and are loadable.
    /// </summary>
    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task AllSalvageMapsLoadableTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var cfg = server.ResolveDependency<IConfigurationManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        await server.WaitPost(() =>
        {
            foreach (var salvage in prototypeManager.EnumeratePrototypes<SalvageMapPrototype>())
            {
                mapSystem.CreateMap(out var mapId);
                try
                {
                    Assert.That(mapLoader.TryLoadGrid(mapId, salvage.MapPath, out var grid));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load salvage map {salvage.ID}, was it saved as a map instead of a grid?", ex);
                }

                try
                {
                    mapSystem.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete salvage map {salvage.ID}", ex);
                }
            }
        });
        await RunUntilSynced();
    }
}
