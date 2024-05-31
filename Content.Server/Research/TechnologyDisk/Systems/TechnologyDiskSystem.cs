using System.Linq;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
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

        var weightedRandom = _prototype.Index<WeightedRandomPrototype>(component.TierWeightPrototype);
        var tier = int.Parse(weightedRandom.Pick(_random));

        //get a list of every distinct recipe in all the technologies.
        var techs = new List<ProtoId<LatheRecipePrototype>>();
        foreach (var tech in _prototype.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Tier != tier)
                continue;

            techs.AddRange(tech.RecipeUnlocks);
        }
        techs = techs.Distinct().ToList();

        if (!techs.Any())
            return;

        //pick one
        component.Recipes = new();
        component.Recipes.Add(_random.Pick(techs));
    }
}
