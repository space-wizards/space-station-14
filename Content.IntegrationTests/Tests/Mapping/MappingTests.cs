using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Mapping;

[TestFixture]
public sealed class MappingTests
{
    /// <summary>
    /// Checks that the mapping command creates paused & uninitialized maps.
    /// </summary>
    [Test]
    public async Task MappingTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, Connected = true, DummyTicker = false });

        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSys = server.System<MapSystem>();

        await pair.RunTicksSync(5);
        var mapId = 1;
        while (mapSys.MapExists(new(mapId)))
        {
            mapId++;
        }

        await pair.WaitClientCommand($"mapping {mapId}");
        var map = mapSys.GetMap(new MapId(mapId));

        var mapXform = server.Transform(map);
        Assert.That(mapXform.MapUid, Is.EqualTo(map));
        Assert.That(mapXform.MapID, Is.EqualTo(new MapId(mapId)));

        var xform = server.Transform(pair.Player!.AttachedEntity!.Value);

        Assert.That(xform.MapUid, Is.EqualTo(map));
        Assert.That(mapSys.IsInitialized(map), Is.False);
        Assert.That(mapSys.IsPaused(map), Is.True);
        Assert.That(server.MetaData(map).EntityLifeStage, Is.EqualTo(EntityLifeStage.Initialized));
        Assert.That(server.MetaData(map).EntityPaused, Is.True);

        // Spawn a new entity
        EntityUid ent = default;
        await server.WaitPost(() =>
        {
            ent = entMan.Spawn(null, new MapCoordinates(default, new(mapId)));
        });
        await pair.RunTicksSync(5);
        Assert.That(server.MetaData(ent).EntityLifeStage, Is.EqualTo(EntityLifeStage.Initialized));
        Assert.That(server.MetaData(ent).EntityPaused, Is.True);

        // Save the map
        var file = $"{nameof(MappingTest)}.yml";
        await pair.WaitClientCommand($"savemap {mapId} {file}");

        // Mapinitialize it
        await pair.WaitClientCommand($"mapinit {mapId}");
        Assert.That(mapSys.IsInitialized(map), Is.True);
        Assert.That(mapSys.IsPaused(map), Is.False);
        Assert.That(server.MetaData(map).EntityLifeStage, Is.EqualTo(EntityLifeStage.MapInitialized));
        Assert.That(server.MetaData(map).EntityPaused, Is.False);
        Assert.That(server.MetaData(ent).EntityLifeStage, Is.EqualTo(EntityLifeStage.MapInitialized));
        Assert.That(server.MetaData(ent).EntityPaused, Is.False);

        await server.WaitPost(() => entMan.DeleteEntity(map));

        // Load the saved map
        mapId++;
        while (mapSys.MapExists(new(mapId)))
        {
            mapId++;
        }

        await pair.WaitClientCommand($"mapping {mapId} {file}");
        map = mapSys.GetMap(new MapId(mapId));

        // And it should all be paused and un-initialized
        xform = server.Transform(pair.Player!.AttachedEntity!.Value);
        Assert.That(xform.MapUid, Is.EqualTo(map));
        Assert.That(mapSys.IsInitialized(map), Is.False);
        Assert.That(mapSys.IsPaused(map), Is.True);
        Assert.That(server.MetaData(map).EntityLifeStage, Is.EqualTo(EntityLifeStage.Initialized));
        Assert.That(server.MetaData(map).EntityPaused, Is.True);

        mapXform = server.Transform(map);
        Assert.That(mapXform.MapUid, Is.EqualTo(map));
        Assert.That(mapXform.MapID, Is.EqualTo(new MapId(mapId)));
        Assert.That(mapXform.ChildCount, Is.EqualTo(2));

        mapXform.ChildEnumerator.MoveNext(out ent);
        if (ent == pair.Player.AttachedEntity)
            mapXform.ChildEnumerator.MoveNext(out ent);

        Assert.That(server.MetaData(ent).EntityLifeStage, Is.EqualTo(EntityLifeStage.Initialized));
        Assert.That(server.MetaData(ent).EntityPaused, Is.True);

        await server.WaitPost(() => entMan.DeleteEntity(map));
        await pair.CleanReturnAsync();
    }
}
