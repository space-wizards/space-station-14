using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared.Research.Events;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedLatheSystem _lathe = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDatabaseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchDisciplineUnlockEvent>(OnResearchDisciplineUnlock);
    }

    private void OnMapInit(EntityUid uid, TechnologyDatabaseComponent component, MapInitEvent args)
    {
        UpdateTechnologyCards(uid, component);
    }

    private void OnResearchDisciplineUnlock(EntityUid uid, TechnologyDatabaseComponent component, ResearchDisciplineUnlockEvent args)
    {
        component.ResearchDisciplineLimit += args.Amount;
        Dirty(uid, component);
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
        Dirty(uid, component);
    }

    public List<TechnologyPrototype> GetAvailableTechnologies(EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return new List<TechnologyPrototype>();

        var availableTechnologies = new List<TechnologyPrototype>();
        var disciplineTiers = GetDisciplineTiers(component);
        foreach (var tech in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
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

        if (tech.RequiresResearchCompletion && !HasCompletedResearch(component))
            return false;

        return true;
    }

    public bool HasCompletedResearch(TechnologyDatabaseComponent component)
    {
        var disciplines = component.MainDisciplines;
        // Each newly purchased discipline must be selected and completed before another upgrade.
        if (disciplines.Count < component.ResearchDisciplineLimit)
            return false;

        foreach (var tech in ProtoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Hidden || tech.RequiresResearchCompletion || !component.SupportedDisciplines.Contains(tech.Discipline))
                continue;

            var discipline = ProtoMan.Index(tech.Discipline);
            if (tech.Tier >= discipline.LockoutTier && !disciplines.Contains(tech.Discipline))
                continue;

            if (!component.UnlockedTechnologies.Contains(tech.ID))
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
        return GetHighestDisciplineTier(component, ProtoMan.Index<TechDisciplinePrototype>(disciplineId));
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        var allTech = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && !p.Hidden && !p.RequiresResearchCompletion).ToList();
        var allUnlocked = new List<TechnologyPrototype>();
        foreach (var recipe in component.UnlockedTechnologies)
        {
            var proto = ProtoMan.Index<TechnologyPrototype>(recipe);
            if (proto.Discipline != techDiscipline.ID || proto.RequiresResearchCompletion)
                continue;
            allUnlocked.Add(proto);
        }

        var highestTier = techDiscipline.TierPrerequisites.Keys.Max();
        var tier = 2; //tier 1 is always given
        var disciplines = component.MainDisciplines;

        // todo this might break if you have hidden technologies. i'm not sure

        while (tier <= highestTier)
        {
            // we need to get the tech for the tier 1 below because that's
            // what the percentage in TierPrerequisites is referring to.
            var unlockedTierCount = allUnlocked.Count(p => p.Tier == tier - 1);
            var allTierCount = allTech.Count(p => p.Tier == tier - 1);

            if (allTierCount == 0)
                break;

            var percent = (float) unlockedTierCount / allTierCount;
            if (percent < techDiscipline.TierPrerequisites[tier])
                break;

            if (tier >= techDiscipline.LockoutTier &&
                !disciplines.Contains(techDiscipline.ID) &&
                disciplines.Count >= component.ResearchDisciplineLimit)
                break;
            tier++;
        }

        return tier - 1;
    }

    public FormattedMessage GetTechnologyDescription(
        TechnologyPrototype technology,
        bool includeCost = true,
        bool includeTier = true,
        bool includePrereqs = false,
        TechDisciplinePrototype? disciplinePrototype = null)
    {
        var description = new FormattedMessage();
        if (includeTier)
        {
            disciplinePrototype ??= ProtoMan.Index(technology.Discipline);
            description.AddMarkupOrThrow(Loc.GetString("research-console-tier-discipline-info",
                ("tier", technology.Tier), ("color", disciplinePrototype.Color), ("discipline", Loc.GetString(disciplinePrototype.Name))));
            description.PushNewline();
        }

        if (includeCost)
        {
            description.AddMarkupOrThrow(Loc.GetString("research-console-cost", ("amount", technology.Cost)));
            description.PushNewline();
        }

        if (includePrereqs && technology.TechnologyPrerequisites.Any())
        {
            description.AddMarkupOrThrow(Loc.GetString("research-console-prereqs-list-start"));
            foreach (var recipe in technology.TechnologyPrerequisites)
            {
                var techProto = ProtoMan.Index(recipe);
                description.PushNewline();
                description.AddMarkupOrThrow(Loc.GetString("research-console-prereqs-list-entry",
                    ("text", Loc.GetString(techProto.Name))));
            }
            description.PushNewline();
        }

        description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-start"));
        foreach (var recipe in technology.RecipeUnlocks)
        {
            var recipeProto = ProtoMan.Index(recipe);
            description.PushNewline();
            description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-entry",
                ("name", _lathe.GetRecipeName(recipeProto))));
        }
        foreach (var generic in technology.GenericUnlocks)
        {
            description.PushNewline();
            description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-entry-generic",
                ("text", Loc.GetString(generic.UnlockDescription))));
        }

        return description;
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

    protected void TryAddMainDiscipline(TechnologyPrototype prototype, EntityUid uid, TechnologyDatabaseComponent component)
    {
        var discipline = ProtoMan.Index(prototype.Discipline);
        if (prototype.Tier < discipline.LockoutTier || prototype.RequiresResearchCompletion ||
            component.MainDisciplines.Contains(prototype.Discipline) ||
            component.MainDisciplines.Count >= component.ResearchDisciplineLimit)
            return;
        component.MainDisciplines.Add(prototype.Discipline);
        Dirty(uid, component);
    }

    /// <summary>
    /// Removes a technology and its recipes from a technology database.
    /// </summary>
    public bool TryRemoveTechnology(Entity<TechnologyDatabaseComponent> entity, ProtoId<TechnologyPrototype> tech)
    {
        return TryRemoveTechnology(entity, ProtoMan.Index(tech));
    }

    /// <summary>
    /// Removes a technology and its recipes from a technology database.
    /// </summary>
    [PublicAPI]
    public bool TryRemoveTechnology(Entity<TechnologyDatabaseComponent> entity, TechnologyPrototype tech)
    {
        if (!entity.Comp.UnlockedTechnologies.Remove(tech.ID))
            return false;

        // check to make sure we didn't somehow get the recipe from another tech.
        // unlikely, but whatever
        var recipes = tech.RecipeUnlocks;
        foreach (var recipe in recipes)
        {
            var hasTechElsewhere = false;
            foreach (var unlockedTech in entity.Comp.UnlockedTechnologies)
            {
                var unlockedTechProto = ProtoMan.Index<TechnologyPrototype>(unlockedTech);

                if (!unlockedTechProto.RecipeUnlocks.Contains(recipe))
                    continue;
                hasTechElsewhere = true;
                break;
            }

            if (!hasTechElsewhere)
                entity.Comp.UnlockedRecipes.Remove(recipe);
        }
        Dirty(entity, entity.Comp);
        UpdateTechnologyCards(entity, entity);
        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(entity.Owner, ref ev);
        return true;
    }

    /// <summary>
    /// Clear all unlocked technologies from the database.
    /// </summary>
    [PublicAPI]
    public void ClearTechs(EntityUid uid, TechnologyDatabaseComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.UnlockedTechnologies.Clear();
        comp.MainDisciplines.Clear();
        comp.ResearchDisciplineLimit = 1;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Adds a lathe recipe to the specified technology database
    /// without checking if it can be unlocked.
    /// </summary>
    public void AddLatheRecipe(EntityUid uid, string recipe, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.UnlockedRecipes.Contains(recipe))
            return;

        component.UnlockedRecipes.Add(recipe);
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent(new List<string> { recipe });
        RaiseLocalEvent(uid, ref ev);
    }
}
