#nullable enable
using System.Threading.Tasks;
using Content.Server.Storage.Components;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class StorageFillTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestStorageFillPrototypes()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<StorageFillComponent>("StorageFill", out var storage)) continue;

                    foreach (var entry in storage.Contents)
                    {
                        var id = entry.PrototypeId;

                        if (id == null)
                        {
                            continue;
                        }

                        Assert.That(protoManager.HasIndex<EntityPrototype>(id), $"Unable to find StorageFill prototype of {id} in prototype {proto.ID}");
                        Assert.That(entry.Amount > 0, $"Specified invalid amount of {entry.Amount} for prototype {proto.ID}");
                        Assert.That(entry.SpawnProbability > 0, $"Specified invalid probability of {entry.SpawnProbability} for prototype {proto.ID}");
                    }
                }
            });
        }
    }
}
