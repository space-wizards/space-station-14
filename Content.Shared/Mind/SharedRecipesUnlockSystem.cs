using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Prototypes;

namespace Content.Shared.Mind;

/// <summary>
/// the system handles the memorization of new recipes by players' minds
/// </summary>
public sealed class SharedRecipeUnlockSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLearnRecipesComponent, MindAddedMessage>(OnMindAdded);

        SubscribeLocalEvent<PaperRecipeTeacherComponent, GetVerbsEvent<ExamineVerb>>(OnRecipeTeacherGetVerbs);
    }

    private void OnRecipeTeacherGetVerbs(Entity<PaperRecipeTeacherComponent> recipeTeacher, ref GetVerbsEvent<ExamineVerb> args)
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

    public void LearnRecipes(MindLearnedRecipesComponent comp, List<ProtoId<ConstructionPrototype>> recipes)
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
