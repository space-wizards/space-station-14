using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

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
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
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
            await pair.CleanReturnAsync();
        }
    }
}
