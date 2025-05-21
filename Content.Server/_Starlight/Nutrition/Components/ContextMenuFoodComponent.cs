using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This component prevents food items from being eaten via self-clicking.
/// They can only be eaten via the context menu.
/// </summary>
[RegisterComponent, Access(typeof(FoodSystem))]
public sealed partial class ContextMenuFoodComponent : Component
{
}
