using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MobMoverComponent : Component
{
    private float _stepSoundDistance;
    [DataField("grabRange")]
    private float _grabRange = 0.6f;
    [DataField("pushStrength")]
    private float _pushStrength = 600f;

    #region Footsteps

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

    #endregion

    #region Movement

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
    ///     Movement speed (m/s) that the entity walks, considering modifiers.
    /// </summary>
    [ViewVariables]
    public float CurrentWalkSpeed => WalkSpeedModifier * BaseWalkSpeed;

    /// <summary>
    ///     Movement speed (m/s) that the entity sprints, considering modifiers.
    /// </summary>
    [ViewVariables]
    public float CurrentSprintSpeed => SprintSpeedModifier * BaseSprintSpeed;

    #endregion

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
}
