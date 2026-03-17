using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This component prevents NPC mobs like mice from wanting to eat something that is edible but is not exactly food.
/// Including but not limited to: uranium, death pills, insulation
/// </summary>
[RegisterComponent]
public sealed partial class BadFoodComponent : Component;
