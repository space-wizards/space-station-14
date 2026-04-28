#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    /// <summary>
    ///     Contains all server entities spawned using GameTest proxy methods.
    /// </summary>
    private readonly List<EntityUid> _serverEntitiesToClean = new();

    /// <summary>
    ///     Contains all client entities spawned using GameTest proxy methods.
    /// </summary>
    private readonly List<EntityUid> _clientEntitiesToClean = new();

    private async Task CleanUpEntities()
    {
        await Task.WhenAll(
            Server.WaitAssertion(() =>
            {
                foreach (var junk in _serverEntitiesToClean)
                {
                    if (!SEntMan.Deleted(junk))
                        SEntMan.DeleteEntity(junk);
                }
            }),
            Client.WaitAssertion(() =>
            {
                foreach (var junk in _clientEntitiesToClean)
                {
                    if (!CEntMan.Deleted(junk))
                        CEntMan.DeleteEntity(junk);
                }
            })
        );
    }

    /// <summary>
    ///     Returns a string representation of an entity for the server.
    /// </summary>
    public string SToPrettyString(EntityUid uid)
    {
        return Pair.Server.EntMan.ToPrettyString(uid);
    }

    /// <summary>
    ///     Returns a string representation of an entity for the client.
    /// </summary>
    public string CToPrettyString(EntityUid uid)
    {
        return Pair.Client.EntMan.ToPrettyString(uid);
    }

    /// <summary>
    ///     Converts a server EntityUid into the client-side equivalent entity.
    /// </summary>
    public EntityUid ToClientUid(EntityUid serverUid)
    {
        return Pair.ToClientUid(serverUid);
    }

    /// <summary>
    ///     Converts a client EntityUid into the server-side equivalent entity.
    /// </summary>
    public EntityUid ToServerUid(EntityUid clientUid)
    {
        return Pair.ToServerUid(clientUid);
    }

    /// <summary>
    ///     Retrieves the given component from an entity on the server.
    /// </summary>
    public T SComp<T>(EntityUid target)
        where T : IComponent
    {
        return SEntMan.GetComponent<T>(target);
    }

    /// <summary>
    ///     Attempts to retrieve the given component from an entity on the server.
    /// </summary>
    public bool STryComp<T>(EntityUid? target, [NotNullWhen(true)] out T? component)
        where T : IComponent
    {
        return SEntMan.TryGetComponent(target, out component);
    }

    /// <summary>
    ///     Retrieves the given component from an entity on the client.
    /// </summary>
    public T CComp<T>(EntityUid target)
        where T : IComponent
    {
        return CEntMan.GetComponent<T>(target);
    }

    /// <summary>
    ///     Attempts to retrieve the given component from an entity on the server.
    /// </summary>
    public bool CTryComp<T>(EntityUid? target, [NotNullWhen(true)] out T? component)
        where T : IComponent
    {
        return SEntMan.TryGetComponent(target, out component);
    }

    /// <summary>
    ///     Pairs an EntityUid with the given component, from the server.
    /// </summary>
    public Entity<T> SEntity<T>(EntityUid target)
        where T : IComponent
    {
        return new(target, SEntMan.GetComponent<T>(target));
    }

    /// <summary>
    ///     Pairs an EntityUid with the given component, from the client.
    /// </summary>
    public Entity<T> CEntity<T>(EntityUid target)
        where T : IComponent
    {
        return new(target, CEntMan.GetComponent<T>(target));
    }

    /// <summary>
    ///     Spawns an entity on the server.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid SSpawn(string? id)
    {
        var res = SEntMan.Spawn(id);
        _serverEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the server at a location.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid SSpawnAtPosition(string? id, EntityCoordinates coordinates)
    {
        var res = SEntMan.SpawnAtPosition(id, coordinates);
        _serverEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the client.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid CSpawn(string? id)
    {
        var res = CEntMan.Spawn(id);
        _clientEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Spawns an entity on the server at a location.
    /// </summary>
    /// <remarks>This tracks the entity for post-test cleanup.</remarks>
    public EntityUid CSpawnAtPosition(string? id, EntityCoordinates coordinates)
    {
        var res = CEntMan.SpawnAtPosition(id, coordinates);
        _clientEntitiesToClean.Add(res);
        return res;
    }

    /// <summary>
    ///     Asynchronously spawns an entity on the server.
    /// </summary>
    public async Task<EntityUid> Spawn(string? id)
    {
        var ent = EntityUid.Invalid;

        await Server.WaitPost(() => ent = SSpawn(id));

        return ent;
    }

    /// <summary>
    ///     Asynchronously spawns an entity on the server at the given position.
    /// </summary>
    public async Task<EntityUid> SpawnAtPosition(string? id, EntityCoordinates coords)
    {
        var ent = EntityUid.Invalid;

        await Server.WaitPost(() => ent = SSpawnAtPosition(id, coords));

        return ent;
    }

    /// <summary>
    ///     Deletes an entity on the server immediately.
    /// </summary>
    public void SDeleteNow(EntityUid id)
    {
        SEntMan.DeleteEntity(id);
    }

    /// <summary>
    ///     Deletes an entity on the client immediately.
    /// </summary>
    public void CDeleteNow(EntityUid id)
    {
        CEntMan.DeleteEntity(id);
    }

    /// <summary>
    ///     Queues an entity for deletion at the end of the tick on the server.
    /// </summary>
    public void SQueueDel(EntityUid id)
    {
        SEntMan.QueueDeleteEntity(id);
    }

    /// <summary>
    ///     Queues an entity for deletion at the end of the tick on the client.
    /// </summary>
    public void CQueueDel(EntityUid id)
    {
        CEntMan.QueueDeleteEntity(id);
    }
}
