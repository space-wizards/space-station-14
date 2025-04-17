#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
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
        where T : IComponent
    {
        var id = Server.ResolveDependency<IComponentFactory>().GetComponentName(typeof(T));
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

    [PublicAPI]
    public Task<IEnumerable<ICommonSession>> AddDummyPlayers(Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities, int count=1)
    {
        return AddDummyPlayers(jobPriorities, jobPriorities.Keys, count);
    }

    [PublicAPI]
    public async Task<IEnumerable<ICommonSession>> AddDummyPlayers(
        Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities,
        IEnumerable<ProtoId<JobPrototype>> jobPreferences,
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
            var newProfile = HumanoidCharacterProfile.Random().WithJobPreferences(jobPreferences).AsEnabled();
            return Task.WhenAll(
                prefMan.SetJobPriorities(s.UserId, jobPriorities),
                prefMan.SetProfile(s.UserId, 0, newProfile));
        });
        _modifiedSessions.UnionWith(sessions.ToHashSet());
        await Server.WaitPost(() => Task.WhenAll(tasks).Wait());
        await RunTicksSync(5);

        return sessions;
    }

    public async Task SetJobPriorities(ICommonSession player,
        Dictionary<ProtoId<JobPrototype>,JobPriority> jobPriorities)
    {
        _modifiedSessions.Add(player);
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            prefMan.SetJobPriorities(player.UserId, jobPriorities).Wait();
        });
        await RunTicksSync(5);
    }

    public Task SetJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities)
    {
        return SetJobPriorities(Player!, jobPriorities);
    }

    public async Task SetJobPreferences(HashSet<ProtoId<JobPrototype>> jobPreferences)
    {
        _modifiedSessions.Add(Player!);
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(Player!.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(Player!.UserId, 0, profile!.WithJobPreferences(jobPreferences)).Wait();
        });
        await RunTicksSync(5);
    }

    public async Task SetAntagPreferences(ICommonSession player, HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        _modifiedSessions.Add(player);
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(player.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(player.UserId, 0, profile!.WithAntagPreferences(antagPreferences)).Wait();
        });
        await RunTicksSync(5);
    }

    public Task SetAntagPreferences(HashSet<ProtoId<AntagPrototype>> antagPreferences)
    {
        return SetAntagPreferences(Player!, antagPreferences);
    }
}
