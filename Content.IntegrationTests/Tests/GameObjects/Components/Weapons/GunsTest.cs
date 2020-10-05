#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Weapon.Ranged;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameObjects.Components.Weapons
{
    [TestFixture]
    public sealed class GunsTest : ContentIntegrationTest
    {
        [Test]
        public async Task TestGuns()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            
            server.Assert(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract)
                        continue;

                    var rangedFound = false;

                    foreach (var rangedType in new[] {"BatteryBarrel", "BoltActionBarrel", "MagazineBarrel", "PumpBarrel", "RevolverBarrel"})
                    {
                        if (proto.Components.ContainsKey(rangedType))
                            rangedFound = true;
                    }

                    if (!rangedFound)
                        continue;
                    
                    var entity = entityManager.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
                    var ranged = entity.GetComponent<SharedRangedWeaponComponent>();
                    
                    Assert.That(ranged.AmmoSpreadRatio <= 1.0f && ranged.AmmoSpreadRatio >= 0.0f);

                }
            });
        }
        
        /// <summary>
        ///     Tests whether the fillprototypes for all guns are valid prototypes
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestFillPrototypes()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();

            server.Assert(() =>
            {
                var mapId = mapManager.CreateMap(new MapId(1));
                
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract)
                        continue;
                    
                    if (proto.Components.ContainsKey("Ammo"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(mapId).Transform.MapPosition);

                        if (!entity.TryGetComponent(out AmmoComponent? ammo))
                            continue;

                        Assert.That(ammo.AmmoIsProjectile || 
                                    protoManager.HasIndex<EntityPrototype>(ammo.ProjectileId), $"{proto.ID} does not have a valid ProjectileID");

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("BatteryBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(mapId).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerBatteryBarrelComponent? battery))
                            continue;

                        Assert.That(protoManager.HasIndex<HitscanPrototype>(battery.AmmoPrototype) ||
                                    protoManager.HasIndex<EntityPrototype>(battery.AmmoPrototype), $"{proto.ID} does not have a valid AmmoPrototype");

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("BoltActionBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(mapId).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerBoltActionBarrelComponent? boltAction))
                            continue;

                        Assert.That(boltAction.FillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(boltAction.FillPrototype), $"{proto.ID} does not have a valid FillPrototype");

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("MagazineBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(mapId).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerMagazineBarrelComponent? magazine))
                            continue;

                        Assert.That(magazine.MagFillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(magazine.MagFillPrototype), $"{proto.ID} does not have a valid MagFillPrototype");

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("PumpBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerPumpBarrelComponent? pump))
                            continue;

                        Assert.That(pump.FillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(pump.FillPrototype), $"{proto.ID} does not have a valid FillPrototype");

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("RangedMagazine"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerRangedMagazineComponent? magazine))
                            continue;

                        Assert.That(magazine.FillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(magazine.FillPrototype), $"{proto.ID} does not have a valid FillPrototype");
                    }
                    
                    if (proto.Components.ContainsKey("RevolverBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerRevolverBarrelComponent? revolver))
                            continue;

                        Assert.That(revolver.FillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(revolver.FillPrototype), $"{proto.ID} does not have a valid FillPrototype");
                    }
                    
                    if (proto.Components.ContainsKey("SpeedLoader"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerSpeedLoaderComponent? speedLoader))
                            continue;

                        Assert.That(speedLoader.FillPrototype == null ||
                                    protoManager.HasIndex<EntityPrototype>(speedLoader.FillPrototype), $"{proto.ID} does not have a valid FillPrototype");
                    }
                }
            });
            
            await server.WaitIdleAsync();
        }

        /// <summary>
        ///     Verifies the paths to gun sounds exist
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestSoundPrototypes()
        {
            var server = StartServerDummyTicker();
            await server.WaitIdleAsync();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var resourceManager = server.ResolveDependency<IResourceManager>();

            server.Assert(() =>
            {
                mapManager.CreateMap(new MapId(1));
                
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract)
                        continue;
                    
                    if (proto.Components.ContainsKey("BatteryBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerBatteryBarrelComponent? battery))
                            continue;

                        foreach (var soundPath in new[]
                        {
                            battery.SoundGunshot,
                            battery.SoundEmpty,
                            
                            battery.SoundPowerCellInsert,
                            battery.SoundPowerCellEject,
                        })
                        {
                            if (soundPath == null)
                                continue;
                            
                            Assert.That(resourceManager.ContentFileExists(soundPath), $"Unable to find file {soundPath} for {proto.ID}");
                        }

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("BoltActionBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerBoltActionBarrelComponent? boltAction))
                            continue;

                        foreach (var soundPath in new[]
                        {
                            boltAction.SoundGunshot, 
                            boltAction.SoundEmpty, 
                            
                            boltAction.SoundRack,
                            boltAction.SoundInsert,
                            boltAction.SoundBoltClosed,
                            boltAction.SoundBoltOpen,
                        })
                        {
                            if (soundPath == null)
                                continue;
                            
                            Assert.That(resourceManager.ContentFileExists(soundPath), $"Unable to find file {soundPath} for {proto.ID}");
                        }

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("MagazineBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerMagazineBarrelComponent? magazine))
                            continue;

                        foreach (var soundPath in new[]
                        {
                            magazine.SoundGunshot,
                            magazine.SoundEmpty,
                            
                            magazine.SoundBoltOpen,
                            magazine.SoundBoltClosed,
                            magazine.SoundRack,
                            magazine.SoundRack,
                            magazine.SoundMagInsert,
                            magazine.SoundMagEject,
                            magazine.SoundAutoEject,
                        })
                        {
                            if (soundPath == null)
                                continue;
                            
                            Assert.That(resourceManager.ContentFileExists(soundPath), $"Unable to find file {soundPath} for {proto.ID}");
                        }

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("PumpBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerPumpBarrelComponent? pump))
                            continue;

                        foreach (var soundPath in new[]
                        {
                            pump.SoundGunshot, 
                            pump.SoundEmpty, 
                            
                            pump.SoundRack,
                            pump.SoundInsert,
                        })
                        {
                            if (soundPath == null)
                                continue;
                            
                            Assert.That(resourceManager.ContentFileExists(soundPath), $"Unable to find file {soundPath} for {proto.ID}");
                        }

                        continue;
                    }
                    
                    if (proto.Components.ContainsKey("RevolverBarrel"))
                    {
                        var entity = entityManager.SpawnEntity(proto.ID, mapManager.GetMapEntity(new MapId(1)).Transform.MapPosition);

                        if (!entity.TryGetComponent(out ServerRevolverBarrelComponent? revolver))
                            continue;

                        foreach (var soundPath in new[]
                        {
                            revolver.SoundGunshot, 
                            revolver.SoundEmpty,
                            
                            revolver.SoundInsert,
                            revolver.SoundEject,
                            revolver.SoundSpin,
                        })
                        {
                            if (soundPath == null)
                                continue;
                            
                            Assert.That(resourceManager.ContentFileExists(soundPath), $"Unable to find file {soundPath} for {proto.ID}");
                        }
                    }
                }
            });
            
            await server.WaitIdleAsync();
        }
    }
}