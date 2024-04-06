using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This component is for clothing and is added to gloves by default.
/// It doesn't make sense for boxing gloves to be the same strength
/// when just being held in hand vs when equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class CanOnlyAttackWhenEquippedComponent : Component
{
}
