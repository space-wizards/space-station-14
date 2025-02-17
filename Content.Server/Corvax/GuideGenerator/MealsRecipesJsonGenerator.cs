using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Kitchen;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Prototypes;
using Content.Server.Construction.Components;
using Content.Server.EntityEffects.Effects;

namespace Content.Server.GuideGenerator;

public sealed class MealsRecipesJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var entities = prototype.EnumeratePrototypes<EntityPrototype>();
        var constructable = prototype.EnumeratePrototypes<ConstructionGraphPrototype>();
        var output = new Dictionary<string, dynamic>();

        var microwaveRecipes =
            prototype
                .EnumeratePrototypes<FoodRecipePrototype>()
                .Select(x => new MicrowaveRecipeEntry(x))
                .ToDictionary(x => x.Id, x => x);


        var sliceableRecipes =
            entities
                .Where(x => x.Components.TryGetComponent("SliceableFood", out var _))
                .Select(x => new SliceRecipeEntry(x))
                .Where(x => x.Result != "") // SOMEONE THOUGHT THAT IT WOULD BE A GREAT IDEA TO PUT COMPONENT ON AN ITEM WITHOUT SPECIFYING THE OUTPUT THING.
                .Where(x => x.Count > 0) // Just in case.
                .ToDictionary(x => x.Id, x => x);


        var grindableRecipes =
            entities
                .Where(x => x.Components.TryGetComponent("Extractable", out var _))
                .Where(x => x.Components.TryGetComponent("SolutionContainerManager", out var _))
                .Where(x => (Regex.Match(x.ID.ToLower().Trim(), @".*[Ff]ood*").Success)) // we dont need some "organ" or "pills" prototypes.
                .Select(x => new GrindRecipeEntry(x))
                .Where(x => x.Result != null)
                .ToDictionary(x => x.Id, x => x);


        // construction-related items start
        var constructionGraphs =
            constructable
                .Where(x => (Regex.Match(x.ID.ToLower().Trim(), @".*.*[Bb]acon*|.*[Ss]teak*|[Pp]izza*|[Tt]ortilla*|[Ee]gg*").Success)) // we only need recipes that has "bacon", "steak", "pizza" "tortilla" and "egg" in it, since they are the only "constructable" recipes
                .ToDictionary(x => x.ID, x => x);

        var constructableEntities = // list of entities which names match regex and has Construction component
            entities
                .Where(x => (Regex.Match(x.ID.ToLower().Trim(), @"(?<![Cc]rate)[Ff]ood*").Success))
                .Where(x => x.Components.ContainsKey("Construction"))
                .ToList();

        var entityGraphs = new Dictionary<string, string>(); // BFH. Since we cannot get component from another .Where call (because of CS0103), let's keep everything in one temp dictionary.

        foreach (var ent in constructableEntities)
        {
            if (ent.Components.TryGetComponent("Construction", out var constructionCompRaw))
            {
                var constructionComp = (ConstructionComponent) constructionCompRaw;
                entityGraphs[ent.ID] = constructionComp.Graph;
            }
        }

        var constructableHeatableEntities = constructableEntities // let's finally create our heatable recipes list
            .Where(x => constructionGraphs.ContainsKey(entityGraphs[x.ID]))
            .Select(x => new HeatableRecipeEntry(constructionGraphs[entityGraphs[x.ID]], x))
            .Where(x => (x.Result != null))
            .Where(x => x.Id != x.Result) // sometimes things dupe (for example if someone puts construction component on both inout and output things)
            .ToDictionary(x => x.Id, x => x);


        var constructableToolableEntities = constructableEntities // let's finally create our toolmade recipes list
            .Where(x => constructionGraphs.ContainsKey(entityGraphs[x.ID]))
            .Select(x => new ToolRecipeEntry(constructionGraphs[entityGraphs[x.ID]], x))
            .Where(x => (x.Result != null))
            .Where(x => x.Id != x.Result) // the same here, things sometimes dupe
            .ToDictionary(x => x.Id, x => x);
        // construction-related items end

        // reaction-related items start
        var reactionPrototypes =
            prototype
                .EnumeratePrototypes<ReactionPrototype>()
                .Select(x => new ReactionEntry(x))
                .ToList();


        var mixableRecipes = new Dictionary<string, Dictionary<string, string>>(); // this is a list because we have https://station14.ru/wiki/Модуль:Chemistry_Lookup that already has everything we need and does everything for us.

        foreach (var react in reactionPrototypes)
        {
            foreach (var effect in react.Effects)
                if (effect.GetType().Equals(typeof(CreateEntityReactionEffect)))
                {
                    var trueEffect = (CreateEntityReactionEffect) effect;
                    if (Regex.Match(trueEffect.Entity.ToLower().Trim(), @".*[Ff]ood*").Success) if (!mixableRecipes.ContainsKey(react.Id))
                        {
                            mixableRecipes[react.Id] = new Dictionary<string, string>();
                            mixableRecipes[react.Id]["id"] = react.Id;
                            mixableRecipes[react.Id]["type"] = "mixableRecipes";
                        }
                }
        }
        // reaction-related items end

        output["microwaveRecipes"] = microwaveRecipes;
        output["sliceableRecipes"] = sliceableRecipes;
        output["grindableRecipes"] = grindableRecipes;
        output["heatableRecipes"] = constructableHeatableEntities;
        output["toolmadeRecipes"] = constructableToolableEntities;
        output["mixableRecipes"] = mixableRecipes;

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        file.Write(JsonSerializer.Serialize(output, serializeOptions));
    }
}
