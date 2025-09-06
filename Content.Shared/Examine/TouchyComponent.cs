using Robust.Shared.GameStates;

namespace Content.Shared.Examine;

/// <summary>
/// While an entity has this component, it'll only be able to examine things within interaction range.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TouchyComponent : Component;
