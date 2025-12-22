#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
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
    /// Set a user's antag preferences. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetAntagPreference(ProtoId<AntagPrototype> id, bool value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is not { } userId)
            return;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = (HumanoidCharacterProfile)prefs.Characters[0];
        var newProfile = profile.WithAntagPreference(id, value);
        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, newProfile).Wait());
    }

    /// <summary>
    /// Set a user's job preferences.  Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetJobPriority(ProtoId<JobPrototype> id, JobPriority value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is { } userId)
            await SetJobPriorities(userId, (id, value));
    }

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(params (ProtoId<JobPrototype>, JobPriority)[] priorities)
        => await SetJobPriorities(Client.User!.Value, priorities);

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(NetUserId user, params (ProtoId<JobPrototype>, JobPriority)[] priorities)
    {
        var highCount = priorities.Count(x => x.Item2 == JobPriority.High);
        Assert.That(highCount, Is.LessThanOrEqualTo(1), "Cannot have more than one high priority job");

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(user);
        var profile = (HumanoidCharacterProfile)prefs.Characters[0];
        var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(profile.JobPriorities);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        if (highCount != 0)
        {
            foreach (var (key, priority) in dictionary)
            {
                if (priority == JobPriority.High)
                    dictionary[key] = JobPriority.Medium;
            }
        }

        foreach (var (job, priority) in priorities)
        {
            if (priority == JobPriority.Never)
                dictionary.Remove(job);
            else
                dictionary[job] = priority;
        }

        var newProfile = profile.WithJobPriorities(dictionary);
        _modifiedProfiles.Add(user);
        await Server.WaitPost(() => prefMan.SetProfile(user, 0, newProfile).Wait());
    }
}
