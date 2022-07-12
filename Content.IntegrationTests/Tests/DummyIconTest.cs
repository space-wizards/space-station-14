#nullable enable
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DummyIconTest
    {
        [Test]
        public async Task Test()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var client = pairTracker.Pair.Client;

            await client.WaitAssertion(() =>
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.NoSpawn || proto.Abstract || !proto.Components.ContainsKey("Sprite")) continue;

                    Assert.DoesNotThrow(() =>
                    {
                        var _ = SpriteComponent.GetPrototypeTextures(proto, resourceCache).ToList();
                    }, "Prototype {0} threw an exception when getting its textures.",
                        proto.ID);
                }
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
