using Robust.Shared.GameStates;

namespace Content.Shared.GreyStation.Weapons.Melee;

/// <summary>
/// Prevents this melee weapon from doing wide swings.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoHeavyAttacksComponent : Component;
