using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Server.Research;

public sealed partial class ResearchSystem
{
    private void InitializeTechnology()
    {
        SubscribeLocalEvent<TechnologyDatabaseComponent, ComponentGetState>(OnTechnologyGetState);
    }

    private void OnTechnologyGetState(EntityUid uid, TechnologyDatabaseComponent component, ref ComponentGetState args)
    {
        args.State = new TechnologyDatabaseState(component.TechnologyIds);
    }

    /// <summary>
    ///     Synchronizes this database against other,
    ///     adding all technologies from the other that
    ///     this one doesn't have.
    /// </summary>
    /// <param name="otherDatabase">The other database</param>
    /// <param name="twoway">Whether the other database should be synced against this one too or not.</param>
    public void Sync(TechnologyDatabaseComponent component, TechnologyDatabaseComponent otherDatabase, bool twoway = true)
    {
        foreach (var tech in otherDatabase.TechnologyIds)
        {
            if (!component.IsTechnologyUnlocked(tech)) AddTechnology(component, tech);
        }

        if (twoway)
            Sync(otherDatabase, component, false);

        Dirty(component);
    }

    /// <summary>
    ///     If there's a research client component attached to the owner entity,
    ///     and the research client is connected to a research server, this method
    ///     syncs against the research server, and the server against the local database.
    /// </summary>
    /// <returns>Whether it could sync or not</returns>
    public bool SyncWithServer(TechnologyDatabaseComponent component, ResearchClientComponent? clientComponent = null)
    {
        if (!Resolve(component.Owner, ref clientComponent, false)) return false;
        if (!TryComp<TechnologyDatabaseComponent>(clientComponent.Server?.Owner, out var clientDatabase)) return false;

        Sync(component, clientDatabase);

        return true;
    }

    /// <summary>
    ///     If possible, unlocks a technology on this database.
    /// </summary>
    /// <param name="technology"></param>
    /// <returns></returns>
    public bool UnlockTechnology(TechnologyDatabaseComponent component, TechnologyPrototype technology)
    {
        if (!component.CanUnlockTechnology(technology)) return false;

        AddTechnology(component, technology.ID);
        Dirty(component);
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    /// <param name="technology"></param>
    public void AddTechnology(TechnologyDatabaseComponent component, string technology)
    {
        component.TechnologyIds.Add(technology);
    }
}
