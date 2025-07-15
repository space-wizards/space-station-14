using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This component allows NPC mobs to eat food with BadFoodComponent.
/// See MobMouseAdmeme for usage.
/// </summary>
[RegisterComponent, Access(typeof(FoodSystem))]
public sealed partial class IgnoreBadFoodComponent : Component
{
}
