#nullable enable
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class StorageFillTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestStorageFillPrototypes()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.Components.TryGetValue("StorageFill", out var storageNode) ||
                        !storageNode.TryGetNode("contents", out YamlSequenceNode? contentsNode)) continue;

                    foreach (var child in contentsNode)
                    {
                        if (child is not YamlMappingNode mapping) continue;
                        var name = mapping.GetNode("name").AsString();
                        Assert.That(protoManager.HasIndex<EntityPrototype>(name), $"Unable to find StorageFill prototype of {name} in prototype {proto.ID}");

                        if (mapping.TryGetNode("amount", out var amount))
                        {
                            Assert.That(amount.AsInt() > 0, $"Specified invalid amount of {amount} for prototype {proto.ID}");
                        }
                    }
                }
            });
        }
    }
}
