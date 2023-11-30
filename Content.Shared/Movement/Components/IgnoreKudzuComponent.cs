using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Special component to allow an entity to navigate kudzu without slowdown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreKudzuComponent : Component
{
}
