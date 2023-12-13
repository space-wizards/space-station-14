using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Sprite;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Kitchen;

/// <summary>
/// Handles finishing meals and setting their metadata
/// </summary>
public sealed class ConstructedMealSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<TagComponent> _tagQuery;

    public override void Initialize()
    {
        base.Initialize();

        _tagQuery = GetEntityQuery<TagComponent>();
    }

    public void Finish(EntityUid uid, ProtoId<ConstructedMealPrototype> meal, string container, string name, string description)
    {
        var items = GetItems(uid, container);
        DebugTools.Assert(items.Count > 0, "Cannot finish empty meal, use ContainerNotEmpty condition");
        var recipe = FindRecipe(items, meal);
        var solution = MealSolution(items, recipe?.BonusSolution);
        if (recipe == null)
        {
            // get the number of each ingredient
            var ingredients = new Dictionary<string, int>();
            foreach (var item in items)
            {
                var meta = MetaData(item);
                // using prototype name to ignore item labels
                var ingredient = meta.EntityPrototype?.Name ?? meta.EntityName;

                if (ingredients.TryGetValue(ingredient, out var count))
                    ingredients[ingredient] = count + 1;
                else
                    ingredients[ingredient] = 1;
            }

            // sort them by how often they are used
            var names = new List<string>(ingredients.Keys);
            names.Sort((a, b) => ingredients[a].CompareTo(ingredients[b]));

            // name shows the most used ingredient, description shows all of them
            name = Loc.GetString(name, ("ingredient", names[0]));
            var values = new List<(string, object)>()
            {
                ("amount", names.Count)
            };

            // needed for a / a and b / a, b and c to work properly
            if (names.Count < 3)
            {
                values.Add(("ingredient1", names[0]));
                if (names.Count > 1)
                    values.Add(("ingredient2", names[1]));
            }
            else
            {
                var last = names[names.Count - 1];
                names.RemoveAt(names.Count - 1);
                values.Add(("list", string.Join(", ", names)));
                values.Add(("last", last));
            }

            description = Loc.GetString(description, values.ToArray());

            // will use automatic sprite from ingredients (trust)
        }
        else
        {
            var proto = _proto.Index<EntityPrototype>(recipe.Prototype);
            name = proto.Name;
            description = proto.Description;

            RaiseNetworkEvent(new CopySpriteEvent(recipe.Prototype, GetNetEntity(uid)));
        }

        var mealMeta = MetaData(uid);
        _meta.SetEntityName(uid, name, mealMeta);
        _meta.SetEntityDescription(uid, description, mealMeta);

        // TODO: copy flavor profile from recipe, then combine non-recipe items' flavors

        // make the meal actually nutritious probably
        // TODO: shared food -> unhardcode
        if (_solutions.TryGetSolution(uid, "food", out var food))
            food.AddSolution(solution);

        // delete meal items now that the meal has proper solution
        if (_container.TryGetContainer(uid, container, out var cont))
            _container.EmptyContainer(cont);
    }

    private IReadOnlyList<EntityUid> GetItems(EntityUid uid, string id)
    {
        if (!_container.TryGetContainer(uid, id, out var container))
            return Array.Empty<EntityUid>();

        return container.ContainedEntities;
    }

    private ConstructedMealRecipePrototype? FindRecipe(IReadOnlyList<EntityUid> items, ProtoId<ConstructedMealPrototype> meal)
    {
        // get all the tag components early
        var tagged = new List<TagComponent>(items.Count);
        foreach (var item in items)
        {
            if (_tagQuery.TryGetComponent(item, out var tag))
                tagged.Add(tag);
        }

        var valid = new List<ConstructedMealRecipePrototype>();
        foreach (var recipe in _proto.EnumeratePrototypes<ConstructedMealRecipePrototype>())
        {
            if (recipe.Meal != meal)
                continue;

            var recipeItems = new List<TagComponent>(tagged);
            if (IsValid(recipe, recipeItems))
                valid.Add(recipe);
        }

        if (valid.Count == 0)
            return null;

        // sort by priority so highest priority recipe is picked
        valid.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return valid[0];
    }

    private bool IsValid(ConstructedMealRecipePrototype recipe, List<TagComponent> items)
    {
        // check that enough items have required tags
        foreach (var (tag, amount) in recipe.Tags)
        {
            for (int n = 0; n < amount; n++)
            {
                // remove found items from the list so they cant be used multiple times
                var missing = true;
                for (int i = 0; i < items.Count; i++)
                {
                    var tagComp = items[i];
                    if (!_tag.HasTag(tagComp, tag))
                        continue;

                    // Vec::swap_remove from rust (🚀)
                    items[i] = items[items.Count - 1];
                    items.RemoveAt(items.Count - 1);

                    missing = false;
                    break;
                }

                if (missing)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Add up the solutions in every item of the meal.
    /// This is added to the food solution, so pizza dough always has value but salad bowls probably don't.
    /// </summary>
    public Solution MealSolution(IReadOnlyList<EntityUid> items, Solution? bonus)
    {
        var result = new Solution();

        foreach (var item in items)
        {
            // TODO: unhardcode solution name when FoodComponent in shared
            if (!_solutions.TryGetSolution(item, "food", out var solution))
                continue;

            result.AddSolution(solution, _proto);
        }

        if (bonus != null)
            result.AddSolution(bonus, _proto);

        return result;
    }
}
