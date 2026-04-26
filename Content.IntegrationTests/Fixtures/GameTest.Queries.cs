// taken from https://github.com/moonheart08/ss14-feature-staging/pull/1

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.EntityQueryEnumerator``1"/>
    public EntityQueryEnumerator<T> SQuery<T>()
        where T : IComponent
    {
        return Server.EntMan.EntityQueryEnumerator<T>();
    }

    /// <inheritdoc cref="M:Robust.Shared.GameObjects.EntityManager.EntityQueryEnumerator``1"/>
    public EntityQueryEnumerator<T> CQuery<T>()
        where T : IComponent
    {
        return Client.EntMan.EntityQueryEnumerator<T>();
    }

    /// <summary>
    ///     Tests whether any entity exists with the given component on the server.
    /// </summary>
    public bool SAnyExists<T>()
        where T : IComponent
    {
        var query = SQuery<T>();

        return query.MoveNext(out _);
    }

    /// <summary>
    ///     Tests whether any entity exists with the given component on the client.
    /// </summary>
    public bool CAnyExists<T>()
        where T : IComponent
    {
        var query = CQuery<T>();

        return query.MoveNext(out _);
    }

    /// <summary>
    ///     Queries the number of entities with a given component on the server.
    /// </summary>
    public int SQueryCount<T>()
        where T : IComponent
    {
        return Server.EntMan.Count<T>();
    }

    /// <summary>
    ///     Queries every entity with the given component on the server and returns a list of them.
    /// </summary>
    public List<Entity<T>> SQueryList<T>()
        where T : IComponent
    {
        var list = new List<Entity<T>>(SQueryCount<T>());

        var q = SQuery<T>();

        while (q.MoveNext(out var ent, out var comp1))
        {
            list.Add((ent, comp1));
        }

        return list;
    }

    /// <summary>
    ///     Queries every entity with the given component on the server and returns a list of them.
    /// </summary>
    public List<Entity<T>> CQueryList<T>()
        where T : IComponent
    {
        var list = new List<Entity<T>>(CQueryCount<T>());

        var q = CQuery<T>();

        while (q.MoveNext(out var ent, out var comp1))
        {
            list.Add((ent, comp1));
        }

        return list;
    }

    /// <summary>
    ///     Queries the number of entities with a given component on the client.
    /// </summary>
    public int CQueryCount<T>()
        where T : IComponent
    {
        return Client.EntMan.Count<T>();
    }

    /// <summary>
    ///     Gets a single instance of an entity with the given component on the server, asserting it is the only one.
    /// </summary>
    public bool SQuerySingle<T>([NotNullWhen(true)] out Entity<T>? ent)
        where T : IComponent
    {
        var query = SQuery<T>();

        if (query.MoveNext(out var eid, out var comp))
        {
            Assert.That(query.MoveNext(out var extra, out _),
                Is.False,
                $"Expected only one entity with {typeof(T)}, found {SToPrettyString(eid)} and then {SToPrettyString(extra)}");
            ent = (eid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    /// <summary>
    ///     Gets a single instance of an entity with the given component on the client, asserting it is the only one.
    /// </summary>
    public bool CQuerySingle<T>([NotNullWhen(true)] out Entity<T>? ent)
        where T : IComponent
    {
        var query = CQuery<T>();

        if (query.MoveNext(out var eid, out var comp))
        {
            Assert.That(query.MoveNext(out var extra, out _),
                Is.False,
                $"Expected only one entity with {typeof(T)}, found {CToPrettyString(eid)} and then {CToPrettyString(extra)}");
            ent = (eid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    public async Task<ICommonSession[]> AddDummySessionsSync(int count = 1)
    {
        var res = await Server.AddDummySessions(count);

        await Pair.ReallyBeIdle(); // That takes a while.

        return res;
    }

    /// <summary>
    ///     Checks whether the given entity has been deleted on the server.
    /// </summary>
    public bool SDeleted(EntityUid? ent)
    {
        return Server.EntMan.Deleted(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has been deleted on the client.
    /// </summary>
    public bool CDeleted(EntityUid? ent)
    {
        return Client.EntMan.Deleted(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has the given component.
    /// </summary>
    public bool SHasComp<T>(EntityUid? ent)
        where T : IComponent
    {
        return Server.EntMan.HasComponent<T>(ent);
    }

    /// <summary>
    ///     Checks whether the given entity has the given component.
    /// </summary>
    public bool CHasComp<T>(EntityUid? ent)
        where T : IComponent
    {
        return Client.EntMan.HasComponent<T>(ent);
    }
}
