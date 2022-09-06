#nullable enable
using System.Threading.Tasks;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Content.Shared.Storage;
using NUnit.Framework;
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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<ServerStorageComponent>("Storage", out var storage) ||
                        storage.Whitelist != null ||
                        !proto.TryGetComponent<ItemComponent>("Item", out var item)) continue;

                    Assert.That(storage.StorageCapacityMax, Is.LessThanOrEqualTo(item.Size), $"Found storage arbitrage on {proto.ID}");
                }
            });
            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestStorageFillPrototypes()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<StorageFillComponent>("StorageFill", out var storage)) continue;

                    foreach (var entry in storage.Contents)
                    {
                        Assert.That(entry.Amount, Is.GreaterThan(0), $"Specified invalid amount of {entry.Amount} for prototype {proto.ID}");
                        Assert.That(entry.SpawnProbability, Is.GreaterThan(0), $"Specified invalid probability of {entry.SpawnProbability} for prototype {proto.ID}");
                    }
                }
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
