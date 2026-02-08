using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Marks melee weapons that should not injure fellow cult members.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultMeleeWeaponComponent : Component
{
}

