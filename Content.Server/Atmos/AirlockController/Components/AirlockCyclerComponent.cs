using Content.Shared.Atmos.AirlockController;

namespace Content.Server.Atmos.AirlockController.Components;

/// <summary>
///     Wallmount outside the airlock that calls the chamber over. Everything here is written
///     by the controller that has it assigned.
/// </summary>
[RegisterComponent]
public sealed partial class AirlockCyclerComponent : Component
{
    /// <summary>
    ///     Null until a controller claims us
    /// </summary>
    [ViewVariables]
    public EntityUid? Controller;

    /// <summary>
    ///     The side we call the chamber to
    /// </summary>
    [ViewVariables]
    public AirlockSide Side;

    [ViewVariables]
    public AirlockCycleStatus Status = new(AirlockCycleState.Idle, AirlockSide.A, null, true, false);

    [ViewVariables]
    public float? ChamberPressure;
}
