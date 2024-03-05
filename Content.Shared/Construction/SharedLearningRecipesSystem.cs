using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Verbs;

namespace Content.Shared.Construction;

public sealed class SharedLearningRecipesSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLearnRecipesComponent, MindAddedMessage>(OnMindAdded);

        SubscribeLocalEvent<RecipeTeacherComponent, GetVerbsEvent<ExamineVerb>>(OnRecipeTeacherGetVerbs);
    }

    private void OnRecipeTeacherGetVerbs(Entity<RecipeTeacherComponent> recipeTeacher, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var mind = _mind.GetMind(args.User);
        if (mind == null)
            return;

        var learned = EnsureComp<MindLearnedRecipesComponent>(mind.Value);
        if (learned == null)
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("lib-book-encrypted-book-verb-text"),
            Message = Loc.GetString("lib-book-encrypted-book-verb-message"),
            Act = () => LearnRecipes(learned, recipeTeacher.Comp.Recipes),
            CloseMenu = true
        });
    }

    private void OnMindAdded(Entity<AutoLearnRecipesComponent> autoLearn, ref MindAddedMessage args)
    {
        var mind = _mind.GetMind(autoLearn);

        if (mind == null)
            return;

        var learned = EnsureComp<MindLearnedRecipesComponent>(mind.Value);
        LearnRecipes(learned, autoLearn.Comp.Recipes);
    }

    public void LearnRecipes(MindLearnedRecipesComponent comp, List<string> recipes)
    {
        foreach (var item in recipes)
        {
            if (comp.LearnedRecipes.Contains(item))
                return;

            comp.LearnedRecipes.Add(item);
        }
    }

    public bool IsMindRecipeLeared(EntityUid mind, string recipe, MindLearnedRecipesComponent? comp = null)
    {
        if (comp == null)
            comp = EnsureComp<MindLearnedRecipesComponent>(mind);

        return comp.LearnedRecipes.Contains(recipe);
    }

    public bool IsUserRecipeLeared(EntityUid user, string recipe, MindLearnedRecipesComponent? comp = null)
    {
        var mind = _mind.GetMind(user);
        if (mind == null)
            return false;

        return IsMindRecipeLeared(mind.Value, recipe);
    }
}
