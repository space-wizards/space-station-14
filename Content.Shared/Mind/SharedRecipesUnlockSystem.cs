using Content.Shared.Construction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Popups;

namespace Content.Shared.Mind;

/// <summary>
/// the system handles the memorization of new recipes by players' minds
/// </summary>
public sealed class SharedRecipeUnlockSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLearnRecipesComponent, MindAddedMessage>(OnMindAdded);

        SubscribeLocalEvent<PaperRecipeTeacherComponent, GetVerbsEvent<ExamineVerb>>(OnRecipeTeacherGetVerbs);
        SubscribeLocalEvent<MindLearnedRecipesComponent, RecipeLearnDoAfterEvent>(OnRecipeLearnedDoAfter);
    }


    private void OnRecipeLearnedDoAfter(Entity<MindLearnedRecipesComponent> learnedRecipes, ref RecipeLearnDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        AddRecipeToMind(learnedRecipes, args.Recipe);
        args.Handled = true;
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

        var user = args.User;

        foreach (var item in recipeTeacher.Comp.Recipes)
        {
            args.Verbs.Add(new()
            {
                Text = Loc.GetString("paper-recipe-learning-verb-text", ("item", _proto.Index(item).Name)),
                Message = Loc.GetString("paper-recipe-learning-verb-message"),
                Act = () => TryLearnRecipe(user, recipeTeacher.Comp, item),
                CloseMenu = true,
            });
        }
    }

    private void OnMindAdded(Entity<AutoLearnRecipesComponent> autoLearn, ref MindAddedMessage args)
    {
        var mind = _mind.GetMind(autoLearn);

        if (mind == null)
            return;

        var learned = EnsureComp<MindLearnedRecipesComponent>(mind.Value);
        AddRecipeToMind(learned, autoLearn.Comp.Recipes);
    }

    public void TryLearnRecipe(EntityUid user, PaperRecipeTeacherComponent teacher, ProtoId<ConstructionPrototype> recipe)
    {
        var mind = _mind.GetMind(user);
        if (mind == null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, teacher.DoAfter, new RecipeLearnDoAfterEvent(recipe), mind, used: user)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            BlockDuplicate = true,
        };
        _doafter.TryStartDoAfter(doAfterArgs);
        _popup.PopupClient(Loc.GetString(teacher.PopupText, ("item", _proto.Index(recipe).Name)), user, user);
    }

    public void AddRecipeToMind(MindLearnedRecipesComponent comp, List<ProtoId<ConstructionPrototype>> recipes)
    {
        foreach (var item in recipes)
            AddRecipeToMind(comp, item);
    }

    public void AddRecipeToMind(MindLearnedRecipesComponent comp, ProtoId<ConstructionPrototype> recipe)
    {
        if (comp.LearnedRecipes.Contains(recipe))
            return;

        comp.LearnedRecipes.Add(recipe);
    }

    public bool IsMindRecipeLearned(EntityUid mind, string recipe, MindLearnedRecipesComponent? comp = null)
    {
        if (comp == null)
            comp = EnsureComp<MindLearnedRecipesComponent>(mind);

        return comp.LearnedRecipes.Contains(recipe);
    }

    public bool IsUserRecipeLearned(EntityUid user, string recipe, MindLearnedRecipesComponent? comp = null)
    {
        var mind = _mind.GetMind(user);
        if (mind == null)
            return false;

        return IsMindRecipeLearned(mind.Value, recipe);
    }
}

[Serializable, NetSerializable]
public sealed partial class RecipeLearnDoAfterEvent : DoAfterEvent
{
    public ProtoId<ConstructionPrototype> Recipe = new();

    public RecipeLearnDoAfterEvent(ProtoId<ConstructionPrototype> recipe)
    {
        Recipe = recipe;
    }
    public override DoAfterEvent Clone() => this;
}
