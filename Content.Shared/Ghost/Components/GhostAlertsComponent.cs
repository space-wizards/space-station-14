using Content.Shared.Teleportation.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Ghost.Components;

/// <summary>
/// It only works with <see cref="AlertTeleportComponent"/>
/// In fact, he simply indicates that ghost alerts should be sent to him.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostAlertsComponent : Component;
