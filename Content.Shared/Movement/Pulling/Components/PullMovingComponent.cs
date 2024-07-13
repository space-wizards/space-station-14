using Robust.Shared.Map;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Added when an entity is being ctrl-click moved when pulled.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PullMovingComponent : Component
{
    // Not serialized to indicate THIS CODE SUCKS, fix pullcontroller first
    // Sorry but I need it here - FaDeOkno
    [ViewVariables]
    public EntityCoordinates MovingTo;
}
