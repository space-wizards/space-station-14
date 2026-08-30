using Robust.Shared.GameStates;

namespace Content.Shared.RatKing.Components;

/// <summary>
/// This is used for entities that can rummage through entities
/// with the <see cref="RummageableComponent"/>
/// </summary>
///
[RegisterComponent, NetworkedComponent]
public sealed partial class RummagerComponent : Component;
