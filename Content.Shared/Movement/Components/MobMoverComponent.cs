using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

/// <summary>
/// The basic mover with all standard fields. Can also handle footstep sounds and weightless movement.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class MobMoverComponent : MoverComponent
{
    public Vector2 CurTickWalkMovement;
    public Vector2 CurTickSprintMovement;

    /// <summary>
    /// The move buttons currently held down. Can be adjusted each subtick.
    /// </summary>
    public MoveButtons HeldMoveButtons = MoveButtons.None;

    private float _stepSoundDistance;
    [DataField("grabRange")]
    private float _grabRange = 0.6f;
    [DataField("pushStrength")]
    private float _pushStrength = 600f;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityCoordinates LastPosition { get; set; }

    /// <summary>
    ///     Used to keep track of how far we have moved before playing a step sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float StepSoundDistance
    {
        get => _stepSoundDistance;
        set
        {
            if (MathHelper.CloseToPercent(_stepSoundDistance, value)) return;
            _stepSoundDistance = value;
        }
    }

    #region Weightless

    [ViewVariables(VVAccess.ReadWrite)]
    public float GrabRange
    {
        get => _grabRange;
        set
        {
            if (MathHelper.CloseToPercent(_grabRange, value)) return;
            _grabRange = value;
            Dirty();
        }
    }

    #endregion

    [ViewVariables(VVAccess.ReadWrite)]
    public float PushStrength
    {
        get => _pushStrength;
        set
        {
            if (MathHelper.CloseToPercent(_pushStrength, value)) return;
            _pushStrength = value;
            Dirty();
        }
    }

    [ViewVariables]
    public Angle LastGridAngle { get; set; } = new(0);

    #region Movement

    // This class has to be able to handle server TPS being lower than client FPS.
    // While still having perfectly responsive movement client side.
    // We do this by keeping track of the exact sub-tick values that inputs are pressed on the client,
    // and then building a total movement vector based on those sub-tick steps.
    //
    // We keep track of the last sub-tick a movement input came in,
    // Then when a new input comes in, we calculate the fraction of the tick the LAST input was active for
    //   (new sub-tick - last sub-tick)
    // and then add to the total-this-tick movement vector
    // by multiplying that fraction by the movement direction for the last input.
    // This allows us to incrementally build the movement vector for the current tick,
    // without having to keep track of some kind of list of inputs and calculating it later.
    //
    // We have to keep track of a separate movement vector for walking and sprinting,
    // since we don't actually know our current movement speed while processing inputs.
    // We change which vector we write into based on whether we were sprinting after the previous input.
    //   (well maybe we do but the code is designed such that MoverSystem applies movement speed)
    //   (and I'm not changing that)

    public const float DefaultBaseWalkSpeed = 3.0f;
    public const float DefaultBaseSprintSpeed = 5.0f;

    [ViewVariables]
    public float WalkSpeedModifier = 1.0f;

    [ViewVariables]
    public float SprintSpeedModifier = 1.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseWalkSpeedVV
    {
        get => BaseWalkSpeed;
        set
        {
            BaseWalkSpeed = value;
            Dirty();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseSprintSpeedVV
    {
        get => BaseSprintSpeed;
        set
        {
            BaseSprintSpeed = value;
            Dirty();
        }
    }

    [DataField("baseWalkSpeed")]
    public float BaseWalkSpeed { get; set; } = DefaultBaseWalkSpeed;

    [DataField("baseSprintSpeed")]
    public float BaseSprintSpeed { get; set; } = DefaultBaseSprintSpeed;

    /// <summary>
    /// BaseWalkSpeed multiplied by WalkSpeedModifier in m/s.
    /// </summary>
    [ViewVariables]
    public float CurrentWalkSpeed => WalkSpeedModifier * BaseWalkSpeed;

    /// <summary>
    /// BaseSprintSpeed multiplied by SprintSpeedModifier in m/s.
    /// </summary>
    [ViewVariables]
    public float CurrentSprintSpeed => SprintSpeedModifier * BaseSprintSpeed;

    public bool Sprinting;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanMove { get; set; } = true;

    #endregion
}
