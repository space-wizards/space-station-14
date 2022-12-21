using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Research.Systems;

public abstract class SharedResearchSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchServerComponent, ComponentGetState>(OnServerGetState);
        SubscribeLocalEvent<ResearchServerComponent, ComponentHandleState>(OnServerHandleState);
        SubscribeLocalEvent<TechnologyDatabaseComponent, ComponentGetState>(OnTechnologyGetState);
        SubscribeLocalEvent<TechnologyDatabaseComponent, ComponentHandleState>(OnTechnologyHandleState);
    }

    private void OnServerGetState(EntityUid uid, ResearchServerComponent component, ref ComponentGetState args)
    {
        args.State = new ResearchServerState(component.ServerName, component.Points, component.Id);
    }

    private void OnServerHandleState(EntityUid uid, ResearchServerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ResearchServerState state)
            return;
        component.ServerName = state.ServerName;
        component.Points = state.Points;
        component.Id = state.Id;
    }

    private void OnTechnologyHandleState(EntityUid uid, TechnologyDatabaseComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TechnologyDatabaseState state)
            return;
        component.TechnologyIds = new (state.Technologies);
        component.RecipeIds = new(state.Recipes);
    }

    private void OnTechnologyGetState(EntityUid uid, TechnologyDatabaseComponent component, ref ComponentGetState args)
    {
        args.State = new TechnologyDatabaseState(component.TechnologyIds, component.RecipeIds);
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component) && IsTechnologyUnlocked(uid, technology.ID, component);
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, string technologyId, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.TechnologyIds.Contains(technologyId);
    }

    /// <summary>
    /// Returns whether or not all the prerequisite
    /// technologies for a technology are unlocked.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="prototype"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the prerequesites are present</returns>
    public bool ArePrerequesitesUnlocked(EntityUid uid, TechnologyPrototype prototype, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        foreach (var technologyId in prototype.RequiredTechnologies)
        {
            if (!IsTechnologyUnlocked(uid, technologyId, component))
                return false;
        }
        return true;
    }
}
