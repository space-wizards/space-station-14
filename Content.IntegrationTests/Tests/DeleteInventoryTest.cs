using System.Threading.Tasks;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
            var entMgr = server.ResolveDependency<IEntityManager>();
            var sysManager = server.ResolveDependency<IEntitySystemManager>();
            var coordinates = testMap.GridCoords;

            await server.WaitAssertion(() =>
            {
                // Spawn everything.
                var invSystem = sysManager.GetEntitySystem<InventorySystem>();

                var container = entMgr.SpawnEntity(null, coordinates);
                entMgr.EnsureComponent<InventoryComponent>(container);
                entMgr.EnsureComponent<ContainerManagerComponent>(container);

                var child = entMgr.SpawnEntity(null, coordinates);
                var item = entMgr.EnsureComponent<ClothingComponent>(child);

                sysManager.GetEntitySystem<ClothingSystem>().SetSlots(child, SlotFlags.HEAD, item);

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
