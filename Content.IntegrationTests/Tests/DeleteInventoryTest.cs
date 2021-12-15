using System.Threading.Tasks;
using Content.Server.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using InventoryComponent = Content.Server.Inventory.Components.InventoryComponent;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class DeleteInventoryTest : ContentIntegrationTest
    {
        // Test that when deleting an entity with an InventoryComponent,
        // any equipped items also get deleted.
        [Test]
        public async Task Test()
        {
            var server = StartServer();

            server.Assert(() =>
            {
                // Spawn everything.
                var mapMan = IoCManager.Resolve<IMapManager>();
                var invSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InventorySystem>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entMgr = IoCManager.Resolve<IEntityManager>();
                var container = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                entMgr.AddComponent<Inventory>(container);

                var child = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                var item = entMgr.AddComponent<SharedItemComponent>(child);
                item.SlotFlags = SlotFlags.HEAD;

                // Equip item.
                Assert.That(invSystem.TryEquip(container, child, "head"), Is.True);

                // Delete parent.
                entMgr.DeleteEntity(container);

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);
            });

            await server.WaitIdleAsync();
        }
    }
}
