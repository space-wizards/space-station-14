using System.Linq;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Research.TechnologyDisk.Systems;

public sealed class TechnologyDiskSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TechnologyDiskComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TechnologyDiskComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TechnologyDiskComponent, MapInitEvent>(OnMapInit);
    }

    private void OnAfterInteract(EntityUid uid, TechnologyDiskComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<ResearchServerComponent>(target) || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        if (component.Recipes != null)
        {
            foreach (var recipe in component.Recipes)
            {
                _research.AddLatheRecipe(target, recipe, database);
            }
        }
        _popup.PopupEntity(Loc.GetString("tech-disk-inserted"), target, args.User);
        QueueDel(uid);
        args.Handled = true;
    }

    private void OnExamine(EntityUid uid, TechnologyDiskComponent component, ExaminedEvent args)
    {
        var message = Loc.GetString("tech-disk-examine-none");
        if (component.Recipes != null && component.Recipes.Any())
        {
            var prototype = _prototype.Index<LatheRecipePrototype>(component.Recipes[0]);
            var resultPrototype = _prototype.Index<EntityPrototype>(prototype.Result);
            message = Loc.GetString("tech-disk-examine", ("result", resultPrototype.Name));

            if (component.Recipes.Count > 1) //idk how to do this well. sue me.
                message += " " + Loc.GetString("tech-disk-examine-more");
        }
        args.PushMarkup(message);
    }

    private void OnMapInit(EntityUid uid, TechnologyDiskComponent component, MapInitEvent args)
    {
        if (component.Recipes != null)
            return;

        var lockoutTiers = new Dictionary<string, int>();
        foreach (var discipline in _prototype.EnumeratePrototypes<TechDisciplinePrototype>())
        {
            lockoutTiers.Add(discipline.ID, discipline.LockoutTier);
        }

        //get a list of every distinct recipe in all the technologies.
        var allTechs = new List<string>();
        foreach (var tech in _prototype.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Tier >= lockoutTiers[tech.Discipline])
                continue;

            allTechs.AddRange(tech.RecipeUnlocks);
        }
        allTechs = allTechs.Distinct().ToList();

        //get a list of every distinct unlocked tech across all databases
        var allUnlocked = new List<string>();
        foreach (var database in EntityQuery<TechnologyDatabaseComponent>())
        {
            allUnlocked.AddRange(database.UnlockedRecipes);
        }
        allUnlocked = allUnlocked.Distinct().ToList();

        //make a list of every single non-unlocked tech
        var validTechs = new List<string>();
        foreach (var tech in allTechs)
        {
            if (allUnlocked.Contains(tech))
                continue;

            validTechs.Add(tech);
        }

        if (!validTechs.Any())
            return;

        //pick one
        component.Recipes = new();
        component.Recipes.Add(_random.Pick(validTechs));
    }
}
