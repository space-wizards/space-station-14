using Robust.Shared.GameStates;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Anyone with this component is immune to being converted into a revolutionary
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RevolutionaryImmuneComponent : Component;
