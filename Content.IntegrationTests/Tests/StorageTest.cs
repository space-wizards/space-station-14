#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class StorageTest
    {
        /// <summary>
        /// Can an item store more than itself weighs.
        /// In an ideal world this test wouldn't need to exist because sizes would be recursive.
        /// </summary>
        [Test]
        public async Task StorageSizeArbitrageTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<StorageComponent>("Storage", out var storage) ||
                        storage.Whitelist != null ||
                        storage.MaxItemSize == null ||
                        !proto.TryGetComponent<ItemComponent>("Item", out var item))
                        continue;

                    Assert.That(storage.MaxItemSize.Value, Is.LessThan(item.Size), $"Found storage arbitrage on {proto.ID}");
                }
            });
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestStorageFillPrototypes()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                    {
                        if (!proto.TryGetComponent<StorageFillComponent>("StorageFill", out var storage))
                            continue;

                        foreach (var entry in storage.Contents)
                        {
                            Assert.That(entry.Amount, Is.GreaterThan(0), $"Specified invalid amount of {entry.Amount} for prototype {proto.ID}");
                            Assert.That(entry.SpawnProbability, Is.GreaterThan(0), $"Specified invalid probability of {entry.SpawnProbability} for prototype {proto.ID}");
                        }
                    }
                });
            });
            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestSufficientSpaceForFill()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var compFact = server.ResolveDependency<IComponentFactory>();
            var id = compFact.GetComponentName(typeof(StorageFillComponent));

            Assert.Multiple(() =>
            {
                foreach (var proto in PoolManager.GetPrototypesWithComponent<StorageFillComponent>(server))
                {
                    if (proto.HasComponent<EntityStorageComponent>(compFact))
                        continue;

                    if (!proto.TryGetComponent<StorageComponent>("Storage", out var storage))
                    {
                        Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");
                        continue;
                    }

                    proto.TryGetComponent<ItemComponent>("Item", out var item);

                    var maxSize = storage.MaxItemSize ??
                                  (item?.Size == null
                                      ? SharedStorageSystem.DefaultStorageMaxItemSize
                                      : (ItemSize) Math.Max(0, (int) item.Size - 1));

                    var capacity = storage.MaxTotalWeight ??
                                   storage.MaxSlots * SharedItemSystem.GetItemSizeWeight(storage.MaxItemSize ?? maxSize);

                    var fill = (StorageFillComponent) proto.Components[id].Component;
                    var size = GetFillSize(fill, false, protoMan);

                    Assert.That(size, Is.LessThanOrEqualTo(capacity), $"{proto.ID} storage fill is too large.");
                    Assert.That(GetFillSize(fill, true, protoMan), Is.LessThanOrEqualTo(storage.MaxSlots), $"{proto.ID} storage fill has too many items.");

                    foreach (var entry in fill.Contents)
                    {
                        if (entry.PrototypeId == null)
                            continue;

                        if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var fillItem))
                            continue;

                        if (!fillItem.TryGetComponent<ItemComponent>("Item", out var entryItem))
                            continue;

                        Assert.That(entryItem.Size, Is.LessThanOrEqualTo(maxSize),
                            $"Entity {proto.ID} has storage-fill item, {entry.PrototypeId}, that is too large");
                    }
                }
            });

            await pair.CleanReturnAsync();

        }

        [Test]
        public async Task TestSufficientSpaceForEntityStorageFill()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var protoMan = server.ResolveDependency<IPrototypeManager>();
            var compFact = server.ResolveDependency<IComponentFactory>();
            var id = compFact.GetComponentName(typeof(StorageFillComponent));

            Assert.Multiple(() =>
            {
                foreach (var proto in PoolManager.GetPrototypesWithComponent<StorageFillComponent>(server))
                {
                    if (proto.HasComponent<StorageComponent>(compFact))
                        continue;

                    if (!proto.TryGetComponent<EntityStorageComponent>("EntityStorage", out var entStorage))
                    {
                        Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");
                        continue;
                    }

                    var fill = (StorageFillComponent) proto.Components[id].Component;
                    var size = GetFillSize(fill, true, protoMan);
                    Assert.That(size, Is.LessThanOrEqualTo(entStorage.Capacity),
                        $"{proto.ID} storage fill is too large.");
                }
            });
            await pair.CleanReturnAsync();
        }

        private int GetEntrySize(EntitySpawnEntry entry, bool getCount, IPrototypeManager protoMan)
        {
            if (entry.PrototypeId == null)
                return 0;

            if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var proto))
            {
                Assert.Fail($"Unknown prototype: {entry.PrototypeId}");
                return 0;
            }

            if (getCount)
                return entry.Amount;

            if (proto.TryGetComponent<ItemComponent>("Item", out var item))
                return SharedItemSystem.GetItemSizeWeight(item.Size) * entry.Amount;

            Assert.Fail($"Prototype is missing item comp: {entry.PrototypeId}");
            return 0;
        }

        private int GetFillSize(StorageFillComponent fill, bool getCount, IPrototypeManager protoMan)
        {
            var totalSize = 0;
            var groups = new Dictionary<string, int>();
            foreach (var entry in fill.Contents)
            {
                var size = GetEntrySize(entry, getCount, protoMan);

                if (entry.GroupId == null)
                    totalSize += size;
                else
                    groups[entry.GroupId] = Math.Max(size, groups.GetValueOrDefault(entry.GroupId));
            }

            return totalSize + groups.Values.Sum();
        }
    }
}
