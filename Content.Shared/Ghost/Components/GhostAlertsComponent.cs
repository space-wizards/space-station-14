using Robust.Shared.GameStates;

namespace Content.Shared.Ghost.Components;

[RegisterComponent]

/// <summary>
/// It only works with AlertTeleportComponent
/// In fact, he simply indicates that ghost alerts should be sent to him.
/// </summary>
public sealed partial class GhostAlertsComponent : Component;
