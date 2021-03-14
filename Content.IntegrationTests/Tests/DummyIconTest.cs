#nullable enable
using System.Linq;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public class DummyIconTest : ContentIntegrationTest
    {
        [Test]
        public void Test()
        {
            var client = StartClient();
            client.WaitIdleAsync();

            var prototypeManager = client.ResolveDependency<IPrototypeManager>();
            var resourceCache = client.ResolveDependency<IResourceCache>();

            client.WaitAssertion(() =>
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
