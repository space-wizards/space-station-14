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

    public void UpdateTechnologyCards(EntityUid uid, TechnologyDatabaseComponent component)
    {
        var availableTechnology = GetAvailableTechnologies(uid, component);

        component.CurrentTechnologyCards.Clear();
        foreach (var discipline in component.SupportedDisciplines)
        {
            var filtered = availableTechnology.
                Where(p => p.Discipline == discipline).ToList();

            if (!filtered.Any())
                continue;

            component.CurrentTechnologyCards.Add(_random.Pick(filtered).ID);
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
        return GetHighestDisciplineTier(component, PrototypeManager.Index<DisciplinePrototype>(disciplineId));
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, DisciplinePrototype discipline)
    {
        var allTech = PrototypeManager.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == discipline.ID && !p.Hidden).ToList();
        var allUnlocked = new List<TechnologyPrototype>();
        foreach (var recipe in component.UnlockedRecipes)
        {
            var proto = PrototypeManager.Index<TechnologyPrototype>(recipe);
            if (proto.Discipline != discipline.ID)
                continue;
            allUnlocked.Add(proto);
        }

        var tier = 1;
        while (true)
        {
            if (!discipline.TierPrerequisites.TryGetValue(tier, out var threshold))
                break;

            var allTier = allTech.Where(p => p.Tier == tier).ToList();
            var unlockedTier = allUnlocked.Where(p => p.Tier == tier).ToList();

            if (allTier.Count == 0 || (float) unlockedTier.Count / allTier.Count < threshold)
                break;

            if (tier >= discipline.LockoutTier &&
                component.MainDiscipline != null &&
                discipline.ID != component.MainDiscipline)
                break;

            tier++;
        }
        return tier - 1;
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, TechnologyPrototype oldTechnology, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component) && IsTechnologyUnlocked(uid, oldTechnology.ID, component);
    }

    /// <summary>
    ///     Returns whether a technology is unlocked on this database or not.
    /// </summary>
    /// <returns>Whether it is unlocked or not</returns>
    public bool IsTechnologyUnlocked(EntityUid uid, string technologyId, TechnologyDatabaseComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.UnlockedTechnologies.Contains(technologyId);
    }
}
