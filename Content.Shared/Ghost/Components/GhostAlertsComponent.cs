using Robust.Shared.GameStates;

namespace Content.Shared.Ghost.Components;

[RegisterComponent, NetworkedComponent]

/// <summary>
/// It only works with <see cref="AlertTeleportComponent"/>
/// In fact, he simply indicates that ghost alerts should be sent to him.
/// </summary>
public sealed partial class GhostAlertsComponent : Component;
