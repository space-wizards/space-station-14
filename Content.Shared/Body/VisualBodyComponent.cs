using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// Component on an entity with <see cref="BodyComponent" /> that modifies its appearance based on contained organs with <see cref="VisualOrganComponent" />
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedVisualBodySystem))]
public sealed partial class VisualBodyComponent : Component;
