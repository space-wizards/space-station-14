using System.Linq;
using System.Numerics;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Server.Storage.EntitySystems;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Roles;

[TestFixture]
public sealed class StartingGearPrototypeStorageTest
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
        var storageSystem = pair.Server.ResolveDependency<StorageSystem>();
        var server = pair.Server;
        var client = pair.Client;

        Assert.That(server.CfgMan.GetCVar(CVars.NetPVS), Is.False);

        var protos = server.ProtoMan
            .EnumeratePrototypes<StartingGearPrototype>()
            .Where(p => !p.Abstract)
            .Where(p => !pair.IsTestPrototype(p))
            .ToList();

        protos.Sort();
        var mapId = MapId.Nullspace;

        await server.WaitPost(() =>
        {
            mapId = mapManager.CreateMap();
        });

        var coords = new MapCoordinates(Vector2.Zero, mapId);

        await pair.RunTicksSync(3);

        foreach (var gearProto in protos)
        {
            var backpackProto = gearProto.GetGear("back");

            EntityUid bag = default;

            await server.WaitPost(() => bag = server.EntMan.SpawnEntity(backpackProto, coords));
            var ents = new ValueList<EntityUid>();

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
                    if (!storageSystem.CanInsert(bag, ent, out _))
                        Assert.Fail($"StartingGearPrototype {gearProto.ID} could not successfully put items into storage {bag.Id}");
                }
            }
        }

        await pair.CleanReturnAsync();
    }
}
