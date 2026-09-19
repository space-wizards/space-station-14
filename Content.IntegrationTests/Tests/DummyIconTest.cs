#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DummyIconTest : GameTest
    {
        [Test]
        public async Task Test()
        {
            var pair = Pair;
            var client = pair.Client;
            var prototypeManager = client.ResolveDependency<IPrototypeManager>();
            var resourceCache = client.ResolveDependency<IResourceCache>();
            var spriteSys = client.System<SpriteSystem>();

            await client.WaitAssertion(() =>
            {
                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.HideSpawnMenu || proto.Abstract || pair.IsTestPrototype(proto) || !proto.Components.ContainsKey("Sprite"))
                        continue;

                    Assert.DoesNotThrow(() =>
                    {
                        var _ = spriteSys.GetPrototypeTextures(proto).ToList();
                    }, "Prototype {0} threw an exception when getting its textures.",
                        proto.ID);
                }
            });
        }
    }
}
