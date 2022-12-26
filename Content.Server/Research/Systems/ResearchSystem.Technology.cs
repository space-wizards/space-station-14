using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    /// Syncs the primary entity's database to that of the secondary entity's database.
    /// </summary>
    public void Sync(EntityUid primaryUid, EntityUid otherUid, TechnologyDatabaseComponent? primaryDb = null, TechnologyDatabaseComponent? otherDb = null)
    {
        if (!Resolve(primaryUid, ref primaryDb) || !Resolve(otherUid, ref otherDb))
            return;

        primaryDb.TechnologyIds = otherDb.TechnologyIds;
        primaryDb.RecipeIds = otherDb.RecipeIds;

        Dirty(primaryDb);

        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(primaryDb.Owner, ref ev);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    public bool SyncClientWithServer(EntityUid uid, TechnologyDatabaseComponent? databaseComponent = null, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(uid, ref databaseComponent, ref clientComponent, false))
            return false;

        if (!TryComp<TechnologyDatabaseComponent>(clientComponent.Server, out var serverDatabase))
            return false;

        Sync(uid, clientComponent.Server.Value, databaseComponent, serverDatabase);
        return true;
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(EntityUid client, string prototypeid, ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!_prototypeManager.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
        {
            Logger.Error("invalid technology prototype");
            return false;
        }
        return UnlockTechnology(client, prototype, component, databaseComponent);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(EntityUid client, TechnologyPrototype prototype, ResearchClientComponent? component = null,
        TechnologyDatabaseComponent? databaseComponent = null)
    {
        if (!Resolve(client, ref component, ref databaseComponent, false))
            return false;

        if (!CanUnlockTechnology(client, prototype, databaseComponent))
            return false;

        if (component.Server is not { } server)
            return false;
        AddTechnology(server, prototype.ID);
        AddPointsToServer(server, -prototype.RequiredPoints);
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_prototypeManager.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(uid, prototype, component);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.TechnologyIds.Add(technology.ID);
        foreach (var unlock in technology.UnlockedRecipes)
        {
            if (component.RecipeIds.Contains(unlock))
                continue;
            component.RecipeIds.Add(unlock);
        }
        Dirty(component);

        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Adds a lathe recipe to the specified technology database
    /// without checking if it can be unlocked.
    /// </summary>
    public void AddLatheRecipe(EntityUid uid, string recipe, TechnologyDatabaseComponent? component = null, bool dirty = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.RecipeIds.Contains(recipe))
            return;

        component.RecipeIds.Add(recipe);
        if (dirty)
            Dirty(component);

        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    public bool CanUnlockTechnology(EntityUid uid, string technology, TechnologyDatabaseComponent? database = null, ResearchClientComponent? client = null)
    {
        if (!_prototypeManager.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return false;
        return CanUnlockTechnology(uid, prototype, database, client);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    public bool CanUnlockTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? database = null, ResearchClientComponent? client = null)
    {
        if (!Resolve(uid, ref database, ref client))
            return false;

        if (!TryGetClientServer(uid, out _, out var serverComponent, client))
            return false;

        if (serverComponent.Points < technology.RequiredPoints)
            return false;

        if (IsTechnologyUnlocked(uid, technology, database))
            return false;

        if (!ArePrerequesitesUnlocked(uid, technology, database))
            return false;
        return true;
    }
}
