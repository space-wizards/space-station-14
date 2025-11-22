using Robust.Shared.GameStates;

namespace Content.Shared.StatusIcon.Components;

/// <summary>
/// Used to indicate a mob can have their job status read by HUDs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JobStatusComponent : Component { }
