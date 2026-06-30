using Robust.Shared.GameStates;

namespace Content.Shared.Buckle.Components;
public sealed partial class BuckleComponent : Component;
/// <summary>
/// Whether or not this can buckled by pacifists
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PacifismDisallowedBuckleComponent : Component;
