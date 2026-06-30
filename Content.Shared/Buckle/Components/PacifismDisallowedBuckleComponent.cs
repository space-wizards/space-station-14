using Robust.Shared.GameStates;

namespace Content.Shared.Buckle.Components;
/// <summary>
/// Whether or not this can buckled by pacifists
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PacifismDisallowedBuckleComponent : Component;
