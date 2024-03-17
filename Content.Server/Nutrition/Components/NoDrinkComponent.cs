using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// This component marks entity, that can't drink anything
/// </summary>

[RegisterComponent, Access(typeof(DrinkSystem))]
public sealed partial class NoDrinkComponent : Component
{
}
    

