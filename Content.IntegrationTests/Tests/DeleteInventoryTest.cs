using Content.Server.Inventory;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DeleteInventoryTest
    {
        // Test that when deleting an entity with an InventoryComponent,
        // any equipped items also get deleted.
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;

            await server.WaitAssertion(() =>
            {
                // Spawn everything.
                var mapMan = IoCManager.Resolve<IMapManager>();
                var invSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InventorySystem>();

                var entMgr = IoCManager.Resolve<IEntityManager>();
                var container = entMgr.SpawnEntity(null, coordinates);
                entMgr.EnsureComponent<ServerInventoryComponent>(container);
                entMgr.EnsureComponent<ContainerManagerComponent>(container);

                var child = entMgr.SpawnEntity(null, coordinates);
                var item = entMgr.EnsureComponent<ClothingComponent>(child);

                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ClothingSystem>().SetSlots(item.Owner, SlotFlags.HEAD, item);

                // Equip item.
                Assert.That(invSystem.TryEquip(container, child, "head"), Is.True);

                // Delete parent.
                entMgr.DeleteEntity(container);

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
