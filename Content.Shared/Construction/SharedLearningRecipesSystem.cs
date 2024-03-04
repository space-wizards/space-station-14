using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;

namespace Content.Shared.Construction;

public sealed class SharedLearningRecipesSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLearnRecipesComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<AutoLearnRecipesComponent> autoLearn, ref MindAddedMessage args)
    {
        var mind = _mind.GetMind(autoLearn);

        if (mind == null)
            return;

        var learned = EnsureComp<LearnedRecipesComponent>(mind.Value);
        foreach (var item in autoLearn.Comp.Recipes)
        {
            LearnRecipe(learned, item);
        }
    }

    public void LearnRecipe(LearnedRecipesComponent comp, string recipe)
    {
        if (comp.LearnedRecipes.Contains(recipe))
            return;

        comp.LearnedRecipes.Add(recipe);
    }

    public bool IsMindRecipeLeared(EntityUid mind, string recipe, LearnedRecipesComponent? comp = null)
    {
        if (comp == null)
            comp = EnsureComp<LearnedRecipesComponent>(mind);

        return comp.LearnedRecipes.Contains(recipe);
    }

    public bool IsUserRecipeLeared(EntityUid user, string recipe, LearnedRecipesComponent? comp = null)
    {
        var mind = _mind.GetMind(user);
        if (mind == null)
            return false;

        return IsMindRecipeLeared(mind.Value, recipe);
    }
}
