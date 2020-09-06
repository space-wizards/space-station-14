using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects.EntitySystems.TileLookup;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class GridTileLookupTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var tileLookup = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GridTileLookupSystem>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            server.Assert(() =>
            {
                List<IEntity> entities;
                var mapOne = mapManager.CreateMap();
                var gridOne = mapManager.CreateGrid(mapOne);

                var tileDefinition = tileDefinitionManager["underplating"];
                var underplating = new Tile(tileDefinition.TileId);
                gridOne.SetTile(new MapIndices(0, 0), underplating);
                gridOne.SetTile(new MapIndices(-1, -1), underplating);

                entities = tileLookup.GetEntitiesIntersecting(gridOne.Index, new MapIndices(0, 0)).ToList();
                Assert.That(entities.Count, Is.EqualTo(0));

                // Space entity, check that nothing intersects it and that also it doesn't throw.
                entityManager.SpawnEntity("HumanMob_Content", new MapCoordinates(Vector2.One * 1000, mapOne));
                entities = tileLookup.GetEntitiesIntersecting(gridOne.Index, new MapIndices(1000, 1000)).ToList();
                Assert.That(entities.Count, Is.EqualTo(0));

                var entityOne = entityManager.SpawnEntity("HumanMob_Content", new EntityCoordinates(gridOne.GridEntityId, Vector2.Zero));
                entityManager.SpawnEntity("HumanMob_Content", new EntityCoordinates(gridOne.GridEntityId, Vector2.One));

                var entityTiles = tileLookup.GetIndices(entityOne);
                Assert.That(entityTiles.Count, Is.EqualTo(2));

                entities = tileLookup.GetEntitiesIntersecting(entityOne).ToList();
                // Includes station entity
                Assert.That(entities.Count, Is.EqualTo(3));

                // Both dummies should be in each corner of the 0,0 tile but only one dummy intersects -1,-1
                entities = tileLookup.GetEntitiesIntersecting(gridOne.Index, new MapIndices(-1, -1)).ToList();
                Assert.That(entities.Count, Is.EqualTo(1));

                entities = tileLookup.GetEntitiesIntersecting(gridOne.Index, new MapIndices(0, 0)).ToList();
                Assert.That(entities.Count, Is.EqualTo(2));
            });

            await server.WaitIdleAsync();
        }
    }
}
