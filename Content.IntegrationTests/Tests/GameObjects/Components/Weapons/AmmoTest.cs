using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Weapons
{
    [TestFixture]
    [TestOf(typeof(SharedAmmoComponent))]
    public sealed class AmmoTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var resourceManager = server.ResolveDependency<IResourceManager>();
            
            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap(new MapId(1));

                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Components.ContainsKey("Ammo"))
                        continue;

                    var entity = entityManager.SpawnEntity(proto.ID,
                        mapManager.GetMapEntity(mapId).Transform.MapPosition);

                    var ammo = entity.GetComponent<SharedAmmoComponent>();
                    Assert.That(resourceManager.ContentFileExists(ammo.ProjectileId));
                    
                    // Can't fire and delete yourself
                    Assert.That(!ammo.AmmoIsProjectile || !ammo.Caseless);
                    
                    Assert.That(ammo.SoundCollectionEject == null || protoManager.Index<SoundCollectionPrototype>(ammo.SoundCollectionEject) != null);
                }
            });
        }
    }
}