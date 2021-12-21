using System.Threading.Tasks;
using Content.Server.Clothing.Components;
using Content.Server.Inventory.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.Inventory.EquipmentSlotDefines;

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

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entMgr = IoCManager.Resolve<IEntityManager>();
                var container = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                var inv = entMgr.AddComponent<InventoryComponent>(container);

                var child = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                var item = entMgr.AddComponent<ClothingComponent>(child);
                item.SlotFlags = SlotFlags.HEAD;

                // Equip item.
                Assert.That(inv.Equip(Slots.HEAD, item, false), Is.True);

                // Delete parent.
                entMgr.DeleteEntity(container);

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);
            });

            await server.WaitIdleAsync();
        }
    }
}
