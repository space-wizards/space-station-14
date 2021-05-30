using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Clothing;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

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
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                // Spawn everything.
                var mapMan = IoCManager.Resolve<IMapManager>();

                mapMan.CreateNewMapEntity(MapId.Nullspace);

                var entMgr = IoCManager.Resolve<IEntityManager>();
                var container = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                var inv = container.AddComponent<InventoryComponent>();

                var child = entMgr.SpawnEntity(null, MapCoordinates.Nullspace);
                var item = child.AddComponent<ClothingComponent>();
                item.SlotFlags = SlotFlags.HEAD;

                // Equip item.
                Assert.That(inv.Equip(Slots.HEAD, item, false), Is.True);

                // Delete parent.
                container.Delete();

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);
            });

            await server.WaitIdleAsync();
        }
    }
}
