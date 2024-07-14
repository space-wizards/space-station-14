using Robust.Shared.Map;

namespace Content.Server.Movement.Components;

/// <summary>
/// Added when an entity is being ctrl-click moved when pulled.
/// </summary>
[RegisterComponent]
public sealed partial class PullMovingComponent : Component
{
    // Not serialized to indicate THIS CODE SUCKS, fix pullcontroller first
    // Sorry but I need it here - FaDeOkno
    // OK I don't really need it so it stays here - FaDeOkno again
    [ViewVariables]
    public EntityCoordinates MovingTo;
}
