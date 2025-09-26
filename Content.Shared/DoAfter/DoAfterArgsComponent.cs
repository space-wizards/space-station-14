using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

/// <summary>
/// For setting DoAfterArgs on an entity level
/// Would require some setup, will require a rework eventually
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoAfterSystem))]
public sealed partial class DoAfterArgsComponent : Component
{
    #region DoAfterArgsSettings
    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.AttemptFrequency"/>
    /// </summary>
    [DataField]
    public AttemptFrequency AttemptFrequency;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Broadcast"/>
    /// </summary>
    [DataField]
    public bool Broadcast;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Delay"/>
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Hidden"/>
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// Should this DoAfter repeat after being completed?
    /// </summary>
    [DataField]
    public bool Repeat;

    #region Break/Cancellation Options
    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.NeedHand"/>
    /// </summary>
    [DataField]
    public bool NeedHand;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnHandChange"/>
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnDropItem"/>
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnWeightlessMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.MovementThreshold"/>
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DistanceThreshold"/>
    /// </summary>
    [DataField]
    public float? DistanceThreshold;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnDamage"/>
    /// </summary>
    [DataField]
    public bool BreakOnDamage;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DamageThreshold"/>
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.RequireCanInteract"/>
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;
    // End Break/Cancellation Options
    #endregion

    /// <summary>
    /// What should the delay be reduced to after completion?
    /// </summary>
    [DataField]
    public TimeSpan? DelayReduction;

    // End DoAfterArgsSettings
    #endregion
}
