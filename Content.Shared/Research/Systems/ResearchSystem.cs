using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Lathe;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Research.Systems;

public sealed partial class ResearchSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedLatheSystem _lathe = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeClient();
        InitializeConsole();
        InitializeSource();
        InitializeServer();

        SubscribeLocalEvent<TechnologyDatabaseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchRegistrationChangedEvent>(OnDatabaseRegistrationChanged);
    }

    /// <summary>
    /// Gets a server based on its unique numeric id.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="id"></param>
    /// <returns>A duple containing server EntityUid and ResearchServerComponent.</returns>
    public bool TryGetServerById(EntityUid client, int id, [NotNullWhen(true)] out Entity<ResearchServerComponent>? server)
    {
        server = null;

        var query = GetServers(client);
        foreach (var ent in query)
        {
            if (ent.Comp.Id != id)
                continue;
            server = ent;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the names of all the servers.
    /// </summary>
    /// <returns></returns>
    public string[] GetServerNames(EntityUid client)
    {
        return GetServers(client).Select(x => x.Comp.ServerName).ToArray();
    }

    /// <summary>
    /// Gets the ids of all the servers
    /// </summary>
    /// <returns></returns>
    public int[] GetServerIds(EntityUid client)
    {
        return GetServers(client).Select(x => x.Comp.Id).ToArray();
    }

    public HashSet<Entity<ResearchServerComponent>> GetServers(EntityUid client)
    {
        var clientXform = Transform(client);
        if (clientXform.GridUid is not { } grid)
            return [];

        var set = new HashSet<Entity<ResearchServerComponent>>();
        _lookup.GetGridEntities(grid, set);
        return set;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out var uid, out var server))
        {
            if (server.NextUpdateTime > _timing.CurTime)
                continue;
            server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

            UpdateServer((uid, server), (int)server.ResearchConsoleUpdateTime.TotalSeconds);
        }
    }

    private void OnMapInit(Entity<TechnologyDatabaseComponent> ent, ref MapInitEvent args)
    {
        UpdateTechnologyCards(ent.AsNullable());
    }

    public void UpdateTechnologyCards(Entity<TechnologyDatabaseComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var availableTechnology = GetAvailableTechnologies(ent);
        _random.Shuffle(availableTechnology);

        ent.Comp.CurrentTechnologyCards.Clear();
        foreach (var discipline in ent.Comp.SupportedDisciplines)
        {
            var selected = availableTechnology.FirstOrDefault(p => p.Discipline == discipline);
            if (selected == null)
                continue;

            ent.Comp.CurrentTechnologyCards.Add(selected.ID);
        }
        Dirty(ent);
    }

    public List<TechnologyPrototype> GetAvailableTechnologies(Entity<TechnologyDatabaseComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [];

        var availableTechnologies = new List<TechnologyPrototype>();
        var disciplineTiers = GetDisciplineTiers(ent.Comp);
        foreach (var tech in _proto.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (IsTechnologyAvailable(ent.Comp, tech, disciplineTiers))
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
        return GetHighestDisciplineTier(component, _proto.Index<TechDisciplinePrototype>(disciplineId));
    }

    public int GetHighestDisciplineTier(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        var allTech = _proto.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && !p.Hidden).ToList();
        var allUnlocked = new List<TechnologyPrototype>();
        foreach (var recipe in component.UnlockedTechnologies)
        {
            var proto = _proto.Index<TechnologyPrototype>(recipe);
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

            var percent = (float)unlockedTierTech.Count / allTierTech.Count;
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
            disciplinePrototype ??= _proto.Index(technology.Discipline);
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
                var techProto = _proto.Index(recipe);
                description.PushNewline();
                description.AddMarkupOrThrow(Loc.GetString("research-console-prereqs-list-entry",
                    ("text", Loc.GetString(techProto.Name))));
            }
            description.PushNewline();
        }

        description.AddMarkupOrThrow(Loc.GetString("research-console-unlocks-list-start"));
        foreach (var recipe in technology.RecipeUnlocks)
        {
            var recipeProto = _proto.Index(recipe);
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

    public void TrySetMainDiscipline(TechnologyPrototype prototype, EntityUid uid, TechnologyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var discipline = _proto.Index(prototype.Discipline);
        if (prototype.Tier < discipline.LockoutTier)
            return;
        component.MainDiscipline = prototype.Discipline;
        Dirty(uid, component);

        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Removes a technology and its recipes from a technology database.
    /// </summary>
    public bool TryRemoveTechnology(Entity<TechnologyDatabaseComponent> entity, ProtoId<TechnologyPrototype> tech)
    {
        return TryRemoveTechnology(entity, _proto.Index(tech));
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
                var unlockedTechProto = _proto.Index(unlockedTech);

                if (!unlockedTechProto.RecipeUnlocks.Contains(recipe))
                    continue;
                hasTechElsewhere = true;
                break;
            }

            if (!hasTechElsewhere)
                entity.Comp.UnlockedRecipes.Remove(recipe);
        }
        Dirty(entity);
        UpdateTechnologyCards(entity.AsNullable());
        return true;
    }

    /// <summary>
    /// Clear all unlocked technologies from the database.
    /// </summary>
    [PublicAPI]
    public void ClearTechs(Entity<TechnologyDatabaseComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.UnlockedTechnologies.Count == 0)
            return;

        ent.Comp.UnlockedTechnologies.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Adds a lathe recipe to the specified technology database
    /// without checking if it can be unlocked.
    /// </summary>
    public void AddLatheRecipe(Entity<TechnologyDatabaseComponent?> ent, string recipe)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.UnlockedRecipes.Contains(recipe))
            return;

        ent.Comp.UnlockedRecipes.Add(recipe);
        Dirty(ent);

        var ev = new TechnologyDatabaseModifiedEvent(new List<string> { recipe });
        RaiseLocalEvent(ent, ref ev);
    }
}
