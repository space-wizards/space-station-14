using Robust.Shared.Map;

namespace Content.Server.Movement.Components;

/// <summary>
/// Added when an entity is being ctrl-click moved when pulled.
/// </summary>
[RegisterComponent]
public sealed partial class PullMovingComponent : Component
{
    // Not serialized to indicate THIS CODE SUCKS, fix pullcontroller first
    [ViewVariables]
    public EntityCoordinates MovingTo;
}
