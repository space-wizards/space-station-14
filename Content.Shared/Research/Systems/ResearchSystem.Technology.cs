using Content.Shared.Database;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Systems;

public partial class ResearchSystem
{
    /// <summary>
    /// Syncs the primary entity's database to that of the secondary entity's database.
    /// </summary>
    /// <param name="primary">The database to be modified during sync</param>
    /// <param name="other">The database to be synced to</param>
    public void Sync(Entity<TechnologyDatabaseComponent?> primary, Entity<TechnologyDatabaseComponent?> other)
    {
        if (!Resolve(primary, ref primary.Comp) || !Resolve(other, ref other.Comp))
            return;

        primary.Comp.MainDiscipline = other.Comp.MainDiscipline;
        primary.Comp.CurrentTechnologyCards = other.Comp.CurrentTechnologyCards;
        primary.Comp.SupportedDisciplines = other.Comp.SupportedDisciplines;
        primary.Comp.UnlockedTechnologies = other.Comp.UnlockedTechnologies;
        primary.Comp.UnlockedRecipes = other.Comp.UnlockedRecipes;

        Dirty(primary);

        var ev = new TechnologyDatabaseSynchronizedEvent();
        RaiseLocalEvent(primary, ref ev);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    public void SyncClientWithServer(Entity<TechnologyDatabaseComponent?, ResearchClientComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        if (!HasComp<TechnologyDatabaseComponent>(ent.Comp2.Server))
            return;

        Sync(ent, ent.Comp2.Server.Value);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(Entity<TechnologyDatabaseComponent?, ResearchClientComponent?> client,
        string prototypeid,
        EntityUid user)
    {
        if (!_proto.TryIndex<TechnologyPrototype>(prototypeid, out var prototype))
            return false;

        return UnlockTechnology(client, prototype, user);
    }

    /// <summary>
    /// Tries to add a technology to a database, checking if it is able to
    /// </summary>
    /// <returns>If the technology was successfully added</returns>
    public bool UnlockTechnology(Entity<TechnologyDatabaseComponent?, ResearchClientComponent?> client,
        TechnologyPrototype prototype,
        EntityUid user)
    {
        if (!Resolve(client, ref client.Comp1, ref client.Comp2, false))
            return false;

        if (!TryGetClientServer((client, client.Comp2), out var server))
            return false;

        if (!CanServerUnlockTechnology(client, prototype))
            return false;

        AddTechnology(server.Value.Owner, prototype);
        TrySetMainDiscipline(prototype, server.Value);
        ModifyServerPoints(server.Value.AsNullable(), -prototype.Cost);
        UpdateTechnologyCards(server.Value.Owner);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user):player} unlocked {prototype.ID} (discipline: {prototype.Discipline}, tier: {prototype.Tier}) at {ToPrettyString(client)}, for server {ToPrettyString(server.Value)}.");
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    [PublicAPI]
    public void AddTechnology(Entity<TechnologyDatabaseComponent?> ent, string technology)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_proto.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(ent, prototype);
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    public void AddTechnology(Entity<TechnologyDatabaseComponent?> ent, TechnologyPrototype technology)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        //todo this needs to support some other stuff, too
        foreach (var generic in technology.GenericUnlocks)
        {
            if (generic.PurchaseEvent != null)
                RaiseLocalEvent(generic.PurchaseEvent);
        }

        ent.Comp.UnlockedTechnologies.Add(technology.ID);
        var addedRecipes = new List<string>();
        foreach (var unlock in technology.RecipeUnlocks)
        {
            if (ent.Comp.UnlockedRecipes.Contains(unlock))
                continue;
            ent.Comp.UnlockedRecipes.Add(unlock);
            addedRecipes.Add(unlock);
        }
        Dirty(ent);

        var ev = new TechnologyDatabaseModifiedEvent(addedRecipes);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    public bool CanServerUnlockTechnology(Entity<TechnologyDatabaseComponent?, ResearchClientComponent?> ent,
        TechnologyPrototype technology)
    {

        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return false;

        if (!TryGetClientServer((ent, ent.Comp2), out var server))
            return false;

        if (!IsTechnologyAvailable(ent.Comp1, technology))
            return false;

        if (technology.Cost > server.Value.Comp.Points)
            return false;

        return true;
    }

    private void OnDatabaseRegistrationChanged(Entity<TechnologyDatabaseComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        if (args.Server != null)
            return;
        ent.Comp.MainDiscipline = null;
        ent.Comp.CurrentTechnologyCards = [];
        ent.Comp.SupportedDisciplines = [];
        ent.Comp.UnlockedTechnologies = [];
        ent.Comp.UnlockedRecipes = [];
        Dirty(ent);
    }
}
