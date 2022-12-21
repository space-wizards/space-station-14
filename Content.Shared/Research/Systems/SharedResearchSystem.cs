using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Systems;

public abstract class SharedResearchSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDatabaseComponent, ComponentGetState>(OnTechnologyGetState);
        SubscribeLocalEvent<TechnologyDatabaseComponent, ComponentHandleState>(OnTechnologyHandleState);
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
    public bool IsTechnologyUnlocked(EntityUid uid, string technologyId, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component) && component.TechnologyIds.Contains(technologyId);
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, TechnologyPrototype technologyId, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component) && IsTechnologyUnlocked(uid, technologyId.ID, component);
    }

    /// <summary>
    ///     Returns whether a technology can be unlocked on this database,
    ///     taking parent technologies into account.
    /// </summary>
    /// <returns>Whether it could be unlocked or not</returns>
    public bool CanUnlockTechnology(EntityUid uid, TechnologyPrototype technology, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (IsTechnologyUnlocked(uid,  technology.ID, component))
            return false;

        foreach (var technologyId in technology.RequiredTechnologies)
        {
            _prototypeManager.TryIndex(technologyId, out TechnologyPrototype? requiredTechnology);
            if (requiredTechnology == null)
                return false;

            if (!IsTechnologyUnlocked(uid, requiredTechnology.ID, component))
                return false;
        }
        return true;
    }
}
