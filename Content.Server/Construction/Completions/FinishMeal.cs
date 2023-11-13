using Content.Shared.Construction;
using Content.Shared.Kitchen;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Completions;

/// <summary>
/// Action that picks the best recipe and sets metadata of the meal to its entity prototype.
/// If a recipe was not made it uses fallback strings with the most common ingredients.
/// </summary>
[DataDefinition]
public sealed partial class FinishMeal : IGraphAction
{
    /// <summary>
    /// Meal prototype to find recipes for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ConstructedMealPrototype> Meal = string.Empty;

    /// <summary>
    /// Container to check for items
    /// </summary>
    [DataField(required: true)]
    public string Container = string.Empty;

    /// <summary>
    /// Fallback name locale id, passed "ingredient" as the most common ingredient in the meal.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// Fallback description locale id, always passed "amount" as the number of ingredients.
    /// If there is 1 ingredient it gets passed ingredient1.
    /// If there are 2 ingredients it gets passed ingredient1 and ingredient2.
    /// If there are more ingredients it gets passed list and last.
    /// </summary>
    [DataField(required: true)]
    public LocId Description = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.System<ConstructedMealSystem>().Finish(uid, Meal, Container, Name, Description);
    }
}
