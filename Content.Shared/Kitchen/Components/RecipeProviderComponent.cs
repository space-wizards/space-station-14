using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///     Applied to entities that have access to secret microwave recipes.
///     See: <see cref="FoodRecipePrototype.SecretRecipe"/>
/// </summary>
[RegisterComponent]
public sealed partial class FoodRecipeProviderComponent : Component
{
    /// <summary>
    /// These are additional recipes that the entity is capable of cooking.
    /// </summary>
    [DataField, ViewVariables]
    public List<ProtoId<FoodRecipePrototype>> ProvidedRecipes = new();
}
