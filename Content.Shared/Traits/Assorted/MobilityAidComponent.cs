using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Component for items that restore normal movement speed when held in-hand for players with the impaired mobility trait.
/// The speed boost is automatically calculated to exactly counteract the player's mobility penalty.
/// Should be used on items that act as mobility aids, such as canes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobilityAidComponent : Component
{
}
