using System.Linq;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Research.Systems;

public abstract class SharedResearchSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDatabaseComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, TechnologyDatabaseComponent component, MapInitEvent args)
    {
        UpdateTechnologyCards(uid, component);
    }

    public void UpdateTechnologyCards(EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var availableTechnology = GetAvailableTechnologies(uid, component);
        _random.Shuffle(availableTechnology);

        component.CurrentTechnologyCards.Clear();
        foreach (var discipline in component.SupportedDisciplines)
        {
            var selected = availableTechnology.FirstOrDefault(p => p.Discipline == discipline);
            if (selected == null)
                continue;

            component.CurrentTechnologyCards.Add(selected.ID);
        }
        Dirty(component);
    }

    public List<TechnologyPrototype> GetAvailableTechnologies(EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return new List<TechnologyPrototype>();

        var availableTechnologies = new List<TechnologyPrototype>();
        var disciplineTiers = GetDisciplineTiers(component);
        foreach (var tech in PrototypeManager.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (IsTechnologyAvailable(component, tech, disciplineTiers))
                availableTechnologies.Add(tech);
        }

        return availableTechnologies;
    }

    public bool IsTechnologyAvailable(TechnologyDatabaseComponent component, TechnologyPrototype tech, Dictionary<string, int>? disciplineTiers = null)
    {
        disciplineTiers ??= GetDisciplineTiers(component);

        if (tech.Hidden)
            return false;

        if (!component.SupportedDisciplines.Contains(tech.Discipline))
            return false;

        if (tech.Tier > disciplineTiers[tech.Discipline])
            return false;

        if (component.UnlockedTechnologies.Contains(tech.ID))
            return false;

        foreach (var prereq in tech.TechnologyPrerequisites)
        {
            if (!component.UnlockedTechnologies.Contains(prereq))
                return false;
        }

        return true;
    }

    public Dictionary<string, int> GetDisciplineTiers(TechnologyDatabaseComponent component)
    {
        var tiers = new Dictionary<string, int>();
        foreach (var discipline in component.SupportedDisciplines)
        {
            tiers.Add(discipline, GetHighestDisciplineTier(component, discipline));
        }

        return tiers;
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, string disciplineId)
    {
        return GetHighestDisciplineTier(component, PrototypeManager.Index<TechDisciplinePrototype>(disciplineId));
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        var allTech = PrototypeManager.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && !p.Hidden).ToList();
        var allUnlocked = new List<TechnologyPrototype>();
        foreach (var recipe in component.UnlockedTechnologies)
        {
            var proto = PrototypeManager.Index<TechnologyPrototype>(recipe);
            if (proto.Discipline != techDiscipline.ID)
                continue;
            allUnlocked.Add(proto);
        }

        var highestTier = techDiscipline.TierPrerequisites.Keys.Max();
        var tier = 2; //tier 1 is always given

        // todo this might break if you have hidden technologies. i'm not sure

        while (tier <= highestTier)
        {
            // we need to get the tech for the tier 1 below because that's
            // what the percentage in TierPrerequisites is referring to.
            var unlockedTierTech = allUnlocked.Where(p => p.Tier == tier - 1).ToList();
            var allTierTech = allTech.Where(p => p.Discipline == techDiscipline.ID && p.Tier == tier - 1).ToList();

            if (allTierTech.Count == 0)
                break;

            var percent = (float) unlockedTierTech.Count / allTierTech.Count;
            if (percent < techDiscipline.TierPrerequisites[tier])
                break;

            if (tier >= techDiscipline.LockoutTier &&
                component.MainDiscipline != null &&
                techDiscipline.ID != component.MainDiscipline)
                break;
            tier++;
        }

        return tier - 1;
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
        return Resolve(uid, ref component, false) && component.UnlockedTechnologies.Contains(technologyId);
    }

    public void TrySetMainDiscipline(TechnologyPrototype prototype, EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var discipline = PrototypeManager.Index<TechDisciplinePrototype>(prototype.Discipline);
        if (prototype.Tier < discipline.LockoutTier)
            return;
        component.MainDiscipline = prototype.Discipline;
        Dirty(uid, component);
    }

    /// <summary>
    /// Clear all unlocked technologies from the database.
    /// </summary>
    public void ClearTechs(EntityUid uid, TechnologyDatabaseComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.UnlockedTechnologies.Count == 0)
            return;

        comp.UnlockedTechnologies.Clear();
        Dirty(uid, comp);
    }
}
