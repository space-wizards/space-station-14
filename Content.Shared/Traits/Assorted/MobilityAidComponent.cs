using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Component to mark items that restore normal movement speed when held in-hand for entities with the impaired mobility trait.
/// The speed is automatically calculated to nullify the entity's speed penalty.
/// Should be used on items that act as mobility aids, such as canes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobilityAidComponent : Component;
