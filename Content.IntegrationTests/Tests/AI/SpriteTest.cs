#nullable enable
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.AI
{
    [TestFixture]
    public sealed class SpriteTest : ContentIntegrationTest
    {
        /// <summary>
        ///     Test RSIs and textures are valid
        /// </summary>
        [Test]
        public async Task TestSpritePaths()
        {
            var (client, server) = await StartConnectedServerClientPair();
            await client.WaitIdleAsync();
            await server.WaitIdleAsync();
            
            var resc = client.ResolveDependency<IResourceCache>();
            var entityManager = client.ResolveDependency<IEntityManager>();
            var mapManager = client.ResolveDependency<IMapManager>();
            var prototypeManager = client.ResolveDependency<IPrototypeManager>();

            await client.WaitIdleAsync();

            client.Assert(() =>
            {
                var mapId = mapManager.CreateMap();
                string filePath;
                var map = mapManager.GetMapEntity(mapId);

                foreach (var proto in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || !proto.Components.ContainsKey("Sprite"))
                        continue;

                    var entity = entityManager.SpawnEntity(proto.ID, map.Transform.MapPosition);
                    var spriteComponent = entity.GetComponent<ISpriteComponent>();

                    foreach (var layer in spriteComponent.AllLayers)
                    {

                        if (layer.RsiState != null && layer.Rsi != null)
                        {
                            filePath = layer.Rsi.Path + "/" + layer.RsiState + ".png";
                            Assert.That(resc.ContentFileExists(filePath), $"Unable to find {filePath}");
                        }
                    }
                }
            });

            await client.WaitIdleAsync();
        }
    }
}