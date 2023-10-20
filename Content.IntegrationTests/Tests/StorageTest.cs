#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

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
                        !proto.TryGetComponent<ItemComponent>("Item", out var item)) continue;

                    Assert.That(storage.StorageCapacityMax, Is.LessThanOrEqualTo(item.Size), $"Found storage arbitrage on {proto.ID}");
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
                    int capacity;
                    var isEntStorage = false;

                    if (proto.TryGetComponent<StorageComponent>("Storage", out var storage))
                    {
                        capacity = storage.StorageCapacityMax;
                    }
                    else if (proto.TryGetComponent<EntityStorageComponent>("EntityStorage", out var entStorage))
                    {
                        capacity = entStorage.Capacity;
                        isEntStorage = true;
                    }
                    else
                    {
                        Assert.Fail($"Entity {proto.ID} has storage-fill without a storage component!");
                        continue;
                    }

                    var fill = (StorageFillComponent) proto.Components[id].Component;
                    var size = GetFillSize(fill, isEntStorage);
                    Assert.That(size, Is.LessThanOrEqualTo(capacity), $"{proto.ID} storage fill is too large.");
                }
            });

            int GetEntrySize(EntitySpawnEntry entry, bool isEntStorage)
            {
                if (entry.PrototypeId == null)
                    return 0;

                if (!protoMan.TryIndex<EntityPrototype>(entry.PrototypeId, out var proto))
                {
                    Assert.Fail($"Unknown prototype: {entry.PrototypeId}");
                    return 0;
                }

                if (isEntStorage)
                    return entry.Amount;

                if (proto.TryGetComponent<ItemComponent>("Item", out var item))
                    return item.Size * entry.Amount;

                Assert.Fail($"Prototype is missing item comp: {entry.PrototypeId}");
                return 0;
            }

            int GetFillSize(StorageFillComponent fill, bool isEntStorage)
            {
                var totalSize = 0;
                var groups = new Dictionary<string, int>();
                foreach (var entry in fill.Contents)
                {
                    var size = GetEntrySize(entry, isEntStorage);

                    if (entry.GroupId == null)
                        totalSize += size;
                    else
                        groups[entry.GroupId] = Math.Max(size, groups.GetValueOrDefault(entry.GroupId));
                }

                return totalSize + groups.Values.Sum();
            }

            await pair.CleanReturnAsync();
        }
    }
}
