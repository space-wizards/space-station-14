using System.Threading.Tasks;
using Content.Server.Clothing.Components;
using Content.Server.Inventory;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DeleteInventoryTest : ContentIntegrationTest
    {
        // Test that when deleting an entity with an InventoryComponent,
        // any equipped items also get deleted.
        [Test]
        public async Task Test()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var invSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<InventorySystem>();

            await server.WaitAssertion(() =>
            {
                // Spawn everything.
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                var container = entManager.SpawnEntity(null, coordinates);
                entManager.AddComponent<ServerInventoryComponent>(container);
                entManager.AddComponent<ContainerManagerComponent>(container);

                var child = entManager.SpawnEntity(null, coordinates);
                var item = entManager.AddComponent<ItemComponent>(child);
                item.SlotFlags = SlotFlags.HEAD;

                // Equip item.
                Assert.That(invSystem.TryEquip(container, child, "head"), Is.True);

                // Delete parent.
                entManager.DeleteEntity(container);

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);
            });
        }
    }
}
