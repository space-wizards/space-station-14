using System.Linq;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    ///     Synchronizes this database against other,
    ///     adding all technologies from the other that
    ///     this one doesn't have.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="otherDatabase">The other database</param>
    /// <param name="twoway">Whether the other database should be synced against this one too or not.</param>
    public void Sync(TechnologyDatabaseComponent component, TechnologyDatabaseComponent otherDatabase, bool twoway = true)
    {
        otherDatabase.TechnologyIds = otherDatabase.TechnologyIds.Union(component.TechnologyIds).ToList();
        otherDatabase.RecipeIds = otherDatabase.RecipeIds.Union(component.RecipeIds).ToList();

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
        if (!Resolve(component.Owner, ref clientComponent, false))
            return false;
        if (!TryComp<TechnologyDatabaseComponent>(clientComponent.Server?.Owner, out var clientDatabase))
            return false;

        Sync(component, clientDatabase);

        return true;
    }

    /// <summary>
    ///     If possible, unlocks a technology on this database.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="technology"></param>
    /// <returns></returns>
    public bool UnlockTechnology(TechnologyDatabaseComponent component, TechnologyPrototype technology)
    {
        if (!CanUnlockTechnology(component.Owner, technology, component)) return false;

        AddTechnology(component, technology.ID);
        return true;
    }

    /// <summary>
    ///     Adds a technology to the database without checking if it could be unlocked.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="technology"></param>
    public void AddTechnology(TechnologyDatabaseComponent component, string technology)
    {
        if (!_prototypeManager.TryIndex<TechnologyPrototype>(technology, out var prototype))
            return;
        AddTechnology(component, prototype);
    }

    public void AddTechnology(TechnologyDatabaseComponent component, TechnologyPrototype technology)
    {
        component.TechnologyIds.Add(technology.ID);
        foreach (var unlock in technology.UnlockedRecipes)
        {
            if (component.RecipeIds.Contains(unlock))
                continue;
            component.RecipeIds.Add(unlock);
        }
        Dirty(component);

        if (!TryComp<ResearchServerComponent>(component.Owner, out var server))
            return;
        foreach (var client in server.Clients)
        {
            if (!TryComp<ResearchConsoleComponent>(client, out var console))
                continue;
            UpdateConsoleInterface(console);
        }
    }

    public void AddLatheRecipe(TechnologyDatabaseComponent component, string recipe, bool dirty = true)
    {
        if (component.RecipeIds.Contains(recipe))
            return;

        component.RecipeIds.Add(recipe);
        if (dirty)
            Dirty(component);
    }
}
