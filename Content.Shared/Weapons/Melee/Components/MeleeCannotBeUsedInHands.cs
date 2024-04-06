using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This component is for clothing and is added to gloves by default.
/// If a entity has this component cannot be used when held in hand.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed partial class MeleeCannotBeUsedInHandsComponent : Component
{
}
