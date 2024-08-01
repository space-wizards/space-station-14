namespace Content.Shared.Explosion.Components;

/// <summary>
///     The component prohibits the player from taking off clothes that has this component.
/// </summary>
/// <remarks>
///     See also ClothingComponent.EquipDelay if you want the clothes that the player cannot take off by himself to be put on by the player with a delay.
///</remarks>
[RegisterComponent]
public sealed partial class SelfUnremovableClothingComponent : Component
{

}
