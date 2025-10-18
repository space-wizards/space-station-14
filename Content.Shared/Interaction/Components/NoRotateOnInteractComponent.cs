using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// This is used for entities which should not rotate on interactions (for instance those who use <see cref="MouseRotator"/> instead)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoRotateOnInteractComponent : Component
{
}
