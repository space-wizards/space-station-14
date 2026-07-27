using Robust.Shared.GameStates;

namespace Content.Shared.Guardian.Components;

/// <summary>
/// Marker component that allows an entity to become a guardian host.
/// Added to entities that may receive a <see cref="GuardianHostComponent"/> through a guardian creator.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanHostGuardianComponent : Component;
