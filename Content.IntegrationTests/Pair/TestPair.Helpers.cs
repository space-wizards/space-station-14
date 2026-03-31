#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    public Task<TestMapData> CreateTestMap(bool initialized = true)
        => CreateTestMap(initialized, "Plating");

    /// <summary>
    /// Loads a test map and returns a <see cref="TestMapData"/> representing it.
    /// </summary>
    /// <param name="testMapPath">The <see cref="ResPath"/> to the test map to load.</param>
    /// <param name="initialized">Whether to initialize the map on load.</param>
    /// <returns>A <see cref="TestMapData"/> representing the loaded map.</returns>
    public async Task<TestMapData> LoadTestMap(ResPath testMapPath, bool initialized = true)
    {
        TestMapData mapData = new();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = initialized };
        var mapLoaderSys = Server.EntMan.System<MapLoaderSystem>();
        var mapSys = Server.System<SharedMapSystem>();

        // Load our test map in and assert that it exists.
        await Server.WaitAssertion(() =>
        {
            Assert.That(mapLoaderSys.TryLoadMap(testMapPath, out var map, out var gridSet, deserializationOptions),
                $"Failed to load map {testMapPath}.");
            Assert.That(gridSet, Is.Not.Empty, "There were no grids loaded from the map!");

            mapData.MapUid = map!.Value.Owner;
            mapData.MapId = map!.Value.Comp.MapId;
            mapData.Grid = gridSet!.First();
            mapData.GridCoords = new EntityCoordinates(mapData.Grid, 0, 0);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapSys.GetAllTiles(mapData.Grid.Owner, mapData.Grid.Comp).First();
        });

        await RunTicksSync(10);
        mapData.CMapUid = ToClientUid(mapData.MapUid);
        mapData.CGridUid = ToClientUid(mapData.Grid);
        mapData.CGridCoords = new EntityCoordinates(mapData.CGridUid, 0, 0);

        return mapData;
    }

    /// <summary>
    /// Add dummy players to the pair with server saved job priority preferences
    /// </summary>
    /// <param name="jobPriorities">Job priorities to initialize the players with</param>
    /// <param name="count">How many players to add</param>
    /// <returns>Enumerable of sessions for the new players</returns>
    [PublicAPI]
    public async Task<IEnumerable<ICommonSession>> AddDummyPlayers(
        Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities,
        int count=1)
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var dbMan = Server.ResolveDependency<UserDbDataManager>();

        var sessions = await Server.AddDummySessions(count);
        await RunTicksSync(5);
        var tasks = sessions.Select(s =>
        {
            // dbMan.ClientConnected(s);
            dbMan.WaitLoadComplete(s).Wait();
            var newProfile = HumanoidCharacterProfile.Random().WithJobPriorities(jobPriorities);
            return prefMan.SetProfile(s.UserId, 0, newProfile);
        });
        await Server.WaitPost(() => Task.WhenAll(tasks).Wait());
        await RunTicksSync(5);

        return sessions;
    }
}
