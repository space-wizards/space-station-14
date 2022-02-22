#nullable enable
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DummyIconTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var (client, _) = await StartConnectedServerClientPair(new ClientContentIntegrationOption(){ Pool = false }, new ServerContentIntegrationOption() { Pool = false });

            var prototypeManager = client.ResolveDependency<IPrototypeManager>();
            var resourceCache = client.ResolveDependency<IResourceCache>();

            await client.WaitAssertion(() =>
            {
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || !proto.Components.ContainsKey("Sprite")) continue;

                    Assert.DoesNotThrow(() =>
                    {
                        var _ = SpriteComponent.GetPrototypeTextures(proto, resourceCache).ToList();
                    }, "Prototype {0} threw an exception when getting its textures.",
                        proto.ID);
                }
            });
        }
    }
}
