using System.Linq;
using System.Numerics;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Content.Shared.Roles;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Content.Server.Storage.EntitySystems;

namespace Content.IntegrationTests.Tests.Roles;

[TestFixture]
public sealed class LoadoutTests
{
    private EntityQuery<StorageComponent> _storageQuery;

    /// <summary>
    /// Checks that a storage fill on a StartingGearPrototype will properly fill
    /// </summary>
    [Test]
    public async Task TestStartingGearStorage()
    {
        var settings = new PoolSettings { Connected = true, Dirty = true };
        await using var pair = await PoolManager.GetServerClient(settings);
        var mapManager = pair.Server.ResolveDependency<IMapManager>();
        var protoManager = pair.Server.ResolveDependency<PrototypeManager>();
        var transformSystem = pair.Server.ResolveDependency<SharedTransformSystem>();
        var storageSystem = pair.Server.ResolveDependency<StorageSystem>();
        var server = pair.Server;
        var client = pair.Client;

        Assert.That(server.CfgMan.GetCVar(CVars.NetPVS), Is.False);

        var protoIds = server.ProtoMan
            .EnumeratePrototypes<StartingGearPrototype>()
            .Where(p => !p.Abstract)
            .Where(p => !pair.IsTestPrototype(p))
            .Select(p => p.ID)
            .ToList();

        protoIds.Sort();
        var mapId = MapId.Nullspace;

        await server.WaitPost(() =>
        {
            mapId = mapManager.CreateMap();
        });

        var coords = new MapCoordinates(Vector2.Zero, mapId);

        await pair.RunTicksSync(3);

        foreach (var protoId in protoIds)
        {
            protoManager.TryIndex(protoId, out StartingGearPrototype gearProto);

            var backpackProto = gearProto.GetGear("Back");

            EntityUid bag = default;

            if (gearProto.Storage.Count > 0)
            {
                if (backpackProto != null)
                {
                    await server.WaitPost(() => bag = server.EntMan.SpawnEntity(backpackProto, coords));

                    var itemcoords = transformSystem.GetMapCoordinates(bag);
                    var ents = new ValueList<EntityUid>();

                    if (_storageQuery.TryComp(bag, out var storage))
                    {
                        foreach (var (slot, entProtos) in gearProto.Storage)
                        {
                            if (entProtos.Count == 0)
                                continue;

                            foreach (var ent in entProtos)
                            {
                                ents.Add(server.EntMan.SpawnEntity(ent, coords));
                            }

                            foreach (var ent in ents)
                            {
                                if (!storageSystem.CanInsert(bag, ent, out _, storage))
                                    Assert.Fail($"StartingGearPrototype {protoId} could not successfully put items into storage {bag.Id}");
                            }
                        }
                    }
                    else
                        Assert.Fail($"StartingGearPrototype {protoId}'s bag {bag.Id} does not have a StorageComponent to fill the storage");
                }
                else
                    Assert.Fail($"StartingGearPrototype {protoId} has a storage field, but does not have a bag to fill");
            }
        }

        await pair.CleanReturnAsync();
    }
}
