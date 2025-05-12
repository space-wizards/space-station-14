#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    /// <summary>
    /// Creates a map, a grid, and a tile, and gives back references to them.
    /// </summary>
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> CreateTestMap(bool initialized = true, string tile = "Plating")
    {
        var mapData = new TestMapData();
        TestMap = mapData;
        await Server.WaitIdleAsync();
        var tileDefinitionManager = Server.ResolveDependency<ITileDefinitionManager>();

        TestMap = mapData;
        await Server.WaitPost(() =>
        {
            mapData.MapUid = Server.System<SharedMapSystem>().CreateMap(out mapData.MapId, runMapInit: initialized);
            mapData.Grid = Server.MapMan.CreateGridEntity(mapData.MapId);
            mapData.GridCoords = new EntityCoordinates(mapData.Grid, 0, 0);
            var plating = tileDefinitionManager[tile];
            var platingTile = new Tile(plating.TileId);
            Server.System<SharedMapSystem>().SetTile(mapData.Grid.Owner, mapData.Grid.Comp, mapData.GridCoords, platingTile);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = Server.System<SharedMapSystem>().GetAllTiles(mapData.Grid.Owner, mapData.Grid.Comp).First();
        });

        TestMap = mapData;
        if (!Settings.Connected)
            return mapData;

        await RunTicksSync(10);
        mapData.CMapUid = ToClientUid(mapData.MapUid);
        mapData.CGridUid = ToClientUid(mapData.Grid);
        mapData.CGridCoords = new EntityCoordinates(mapData.CGridUid, 0, 0);

        TestMap = mapData;
        return mapData;
    }

    /// <summary>
    /// Convert a client-side uid into a server-side uid
    /// </summary>
    public EntityUid ToServerUid(EntityUid uid) => ConvertUid(uid, Client, Server);

    /// <summary>
    /// Convert a server-side uid into a client-side uid
    /// </summary>
    public EntityUid ToClientUid(EntityUid uid) => ConvertUid(uid, Server, Client);

    private static EntityUid ConvertUid(
        EntityUid uid,
        RobustIntegrationTest.IntegrationInstance source,
        RobustIntegrationTest.IntegrationInstance destination)
    {
        if (!uid.IsValid())
            return EntityUid.Invalid;

        if (!source.EntMan.TryGetComponent<MetaDataComponent>(uid, out var meta))
        {
            Assert.Fail($"Failed to resolve MetaData while converting the EntityUid for entity {uid}");
            return EntityUid.Invalid;
        }

        if (!destination.EntMan.TryGetEntity(meta.NetEntity, out var otherUid))
        {
            Assert.Fail($"Failed to resolve net ID while converting the EntityUid entity {source.EntMan.ToPrettyString(uid)}");
            return EntityUid.Invalid;
        }

        return otherUid.Value;
    }

    /// <summary>
    /// Execute a command on the server and wait some number of ticks.
    /// </summary>
    public async Task WaitCommand(string cmd, int numTicks = 10)
    {
        await Server.ExecuteCommand(cmd);
        await RunTicksSync(numTicks);
    }

    /// <summary>
    /// Execute a command on the client and wait some number of ticks.
    /// </summary>
    public async Task WaitClientCommand(string cmd, int numTicks = 10)
    {
        await Client.ExecuteCommand(cmd);
        await RunTicksSync(numTicks);
    }

    /// <summary>
    /// Retrieve all entity prototypes that have some component.
    /// </summary>
    public List<(EntityPrototype, T)> GetPrototypesWithComponent<T>(
        HashSet<string>? ignored = null,
        bool ignoreAbstract = true,
        bool ignoreTestPrototypes = true)
        where T : IComponent, new()
    {
        if (!Server.ResolveDependency<IComponentFactory>().TryGetRegistration<T>(out var reg)
            && !Client.ResolveDependency<IComponentFactory>().TryGetRegistration<T>(out reg))
        {
            Assert.Fail($"Unknown component: {typeof(T).Name}");
            return new();
        }

        var id = reg.Name;
        var list = new List<(EntityPrototype, T)>();
        foreach (var proto in Server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ignored != null && ignored.Contains(proto.ID))
                continue;

            if (ignoreAbstract && proto.Abstract)
                continue;

            if (ignoreTestPrototypes && IsTestPrototype(proto))
                continue;

            if (proto.Components.TryGetComponent(id, out var cmp))
                list.Add((proto, (T)cmp));
        }

        return list;
    }

    /// <summary>
    /// Retrieve all entity prototypes that have some component.
    /// </summary>
    public List<EntityPrototype> GetPrototypesWithComponent(Type type,
        HashSet<string>? ignored = null,
        bool ignoreAbstract = true,
        bool ignoreTestPrototypes = true)
    {
        var id = Server.ResolveDependency<IComponentFactory>().GetComponentName(type);
        var list = new List<EntityPrototype>();
        foreach (var proto in Server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ignored != null && ignored.Contains(proto.ID))
                continue;

            if (ignoreAbstract && proto.Abstract)
                continue;

            if (ignoreTestPrototypes && IsTestPrototype(proto))
                continue;

            if (proto.Components.ContainsKey(id))
                list.Add((proto));
        }

        return list;
    }

    /// <summary>
    /// Set a user's antag preferences. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetAntagPreference(ProtoId<AntagPrototype> id, bool value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is not {} userId)
            return;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = (HumanoidCharacterProfile) prefs.Characters[0];
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
        var profile = (HumanoidCharacterProfile) prefs.Characters[0];
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
