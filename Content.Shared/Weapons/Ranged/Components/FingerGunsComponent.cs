namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Marks the glove entity as finger guns, swapping to <see cref="FingerGunsGunComponent"/> when used in hand.
/// The actual swap is handled by <c>TransformableItemComponent</c>.
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsComponent : Component
{
}

/// <summary>
/// Marks the hidden gun entity as the finger guns weapon, swapping back to <see cref="FingerGunsComponent"/>
/// via an alternative verb. The actual swap is handled by <c>TransformableItemComponent</c>.
/// </summary>
[RegisterComponent]
public sealed partial class FingerGunsGunComponent : Component
{
}
