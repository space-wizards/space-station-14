using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DoAfterArgs
{
    /// <summary>
    ///     The entity invoking do_after
    /// </summary>
    [NonSerialized]
    [DataField("user", required: true)]
    public EntityUid User;

    public NetEntity NetUser;

    /// <summary>
    ///     How long does the do_after require to complete
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Delay;

    /// <summary>
    ///     Applicable target (if relevant)
    /// </summary>
    [NonSerialized]
    [DataField]
    public EntityUid? Target;

    public NetEntity? NetTarget;

    /// <summary>
    ///     Entity used by the User on the Target.
    /// </summary>
    [NonSerialized]
    [DataField("using")]
    public EntityUid? Used;

    public NetEntity? NetUsed;

    /// <summary>
    /// Whether the progress bar for this DoAfter should be hidden from other players.
    /// </summary>
    [DataField]
    public bool Hidden;

    #region Event options
    /// <summary>
    ///     The event that will get raised when the DoAfter has finished. If null, this will simply raise a <see cref="SimpleDoAfterEvent"/>
    /// </summary>
    [DataField(required: true)]
    public DoAfterEvent Event = default!;

    /// <summary>
    ///     This option determines how frequently the DoAfterAttempt event will get raised. Defaults to never raising the
    ///     event.
    /// </summary>
    [DataField("attemptEventFrequency")]
    public AttemptFrequency AttemptFrequency;

    /// <summary>
    ///     Entity which will receive the directed event. If null, no directed event will be raised.
    /// </summary>
    [NonSerialized]
    [DataField]
    public EntityUid? EventTarget;

    public NetEntity? NetEventTarget;

    /// <summary>
    /// Should the DoAfter event broadcast? If this is false, then <see cref="EventTarget"/> should be a valid entity.
    /// </summary>
    [DataField]
    public bool Broadcast;
    #endregion

    #region Break/Cancellation Options
    // Break the chains
    /// <summary>
    ///     Whether or not this do after requires the user to have hands.
    /// </summary>
    [DataField]
    public bool NeedHand;

    /// <summary>
    ///     Whether we need to keep our active hand as is (i.e. can't change hand or change item). This also covers
    ///     requiring the hand to be free (if applicable). This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    ///     Whether the do-after should get interrupted if we drop the
    ///     active item we started the do-after with
    ///     This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    ///     If do_after stops when the user or target moves
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    ///     Whether to break on movement when the user is weightless.
    ///     This does nothing if <see cref="BreakOnMove"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = true;

    /// <summary>
    ///     Threshold for user and target movement
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    ///     Threshold for distance user from the used OR target entities.
    /// </summary>
    [DataField]
    public float? DistanceThreshold;

    /// <summary>
    ///     Whether damage will cancel the DoAfter. See also <see cref="DamageThreshold"/>.
    /// </summary>
    [DataField]
    public bool BreakOnDamage;

    /// <summary>
    ///     Threshold for user damage. This damage has to be dealt in a single event, not over time.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    /// <summary>
    ///     If true, this DoAfter will be canceled if the user can no longer interact with the target.
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;
    #endregion

    #region Duplicates
    /// <summary>
    ///     If true, this will prevent duplicate DoAfters from being started See also <see cref="DuplicateConditions"/>.
    /// </summary>
    /// <remarks>
    ///     Note that this will block even if the duplicate is cancelled because either DoAfter had
    ///     <see cref="CancelDuplicate"/> enabled.
    /// </remarks>
    [DataField]
    public bool BlockDuplicate = true;

    //TODO: User pref to not cancel on second use on specific doafters
    /// <summary>
    ///     If true, this will cancel any duplicate DoAfters when attempting to add a new DoAfter. See also
    ///     <see cref="DuplicateConditions"/>.
    /// </summary>
    [DataField]
    public bool CancelDuplicate = true;

    /// <summary>
    ///     These flags determine what DoAfter properties are used to determine whether one DoAfter is a duplicate of
    ///     another.
    /// </summary>
    /// <remarks>
    ///     Note that both DoAfters may have their own conditions, and they will be considered duplicated if either set
    ///     of conditions is satisfied.
    /// </remarks>
    [DataField]
    public DuplicateConditions DuplicateCondition = DuplicateConditions.All;
    #endregion

    /// <summary>
    ///     Additional conditions that need to be met. Return false to cancel.
    /// </summary>
    [NonSerialized]
    [Obsolete("Use checkEvent instead")]
    public Func<bool>? ExtraCheck;

    #region Constructors

    /// <summary>
    ///     Creates a new set of DoAfter arguments.
    /// </summary>
    /// <param name="user">The user that will perform the DoAfter</param>
    /// <param name="delay">The time it takes for the DoAfter to complete</param>
    /// <param name="event">The event that will be raised when the DoAfter has ended (completed or cancelled).</param>
    /// <param name="eventTarget">The entity at which the event will be directed. If null, the event will not be directed.</param>
    /// <param name="target">The entity being targeted by the DoAFter. Not the same as <see cref="EventTarget"/></param>.
    /// <param name="used">The entity being used during the DoAfter. E.g., a tool</param>
    public DoAfterArgs(
        IEntityManager entManager,
        EntityUid user,
        TimeSpan delay,
        DoAfterEvent @event,
        EntityUid? eventTarget,
        EntityUid? target = null,
        EntityUid? used = null)
    {
        User = user;
        Delay = delay;
        Target = target;
        Used = used;
        EventTarget = eventTarget;
        Event = @event;

        NetUser = entManager.GetNetEntity(User);
        NetTarget = entManager.GetNetEntity(Target);
        NetUsed = entManager.GetNetEntity(Used);
    }

    private DoAfterArgs()
    {
    }

    /// <summary>
    ///     Creates a new set of DoAfter arguments.
    /// </summary>
    /// <param name="user">The user that will perform the DoAfter</param>
    /// <param name="seconds">The time it takes for the DoAfter to complete, in seconds</param>
    /// <param name="event">The event that will be raised when the DoAfter has ended (completed or cancelled).</param>
    /// <param name="eventTarget">The entity at which the event will be directed. If null, the event will not be directed.</param>
    /// <param name="target">The entity being targeted by the DoAfter. Not the same as <see cref="EventTarget"/></param>.
    /// <param name="used">The entity being used during the DoAfter. E.g., a tool</param>
    public DoAfterArgs(
        IEntityManager entManager,
        EntityUid user,
        float seconds,
        DoAfterEvent @event,
        EntityUid? eventTarget,
        EntityUid? target = null,
        EntityUid? used = null)
        : this(entManager, user, TimeSpan.FromSeconds(seconds), @event, eventTarget, target, used)
    {
    }

    #endregion

    //The almighty pyramid returns.......
    public DoAfterArgs(DoAfterArgs other)
    {
        User = other.User;
        Delay = other.Delay;
        Target = other.Target;
        Used = other.Used;
        Hidden = other.Hidden;
        EventTarget = other.EventTarget;
        Broadcast = other.Broadcast;
        NeedHand = other.NeedHand;
        BreakOnHandChange = other.BreakOnHandChange;
        BreakOnDropItem = other.BreakOnDropItem;
        BreakOnMove = other.BreakOnMove;
        BreakOnWeightlessMove = other.BreakOnWeightlessMove;
        MovementThreshold = other.MovementThreshold;
        DistanceThreshold = other.DistanceThreshold;
        BreakOnDamage = other.BreakOnDamage;
        DamageThreshold = other.DamageThreshold;
        RequireCanInteract = other.RequireCanInteract;
        AttemptFrequency = other.AttemptFrequency;
        BlockDuplicate = other.BlockDuplicate;
        CancelDuplicate = other.CancelDuplicate;
        DuplicateCondition = other.DuplicateCondition;

        // Networked
        NetUser = other.NetUser;
        NetTarget = other.NetTarget;
        NetUsed = other.NetUsed;
        NetEventTarget = other.NetEventTarget;

        Event = other.Event.Clone();
    }
}

/// <summary>
///     See <see cref="DoAfterArgs.DuplicateCondition"/>.
/// </summary>
[Flags]
public enum DuplicateConditions : byte
{
    /// <summary>
    ///     This DoAfter will consider any other DoAfter with the same user to be a duplicate.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Requires that <see cref="Used"/> refers to the same entity in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    ///     E.g., if all checks are enabled for stripping, then stripping different articles of clothing on the same
    ///     mob would be allowed. If instead this check were disabled, then any stripping actions on the same target
    ///     would be considered duplicates, so you would only be able to take one piece of clothing at a time.
    /// </remarks>
    SameTool = 1 << 1,

    /// <summary>
    ///     Requires that <see cref="Target"/> refers to the same entity in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    ///     E.g., if all checks are enabled for mining, then using the same pickaxe to mine different rocks will be
    ///     allowed. If instead this check were disabled, then the trying to mine a different rock with the same
    ///     pickaxe would be considered a duplicate DoAfter.
    /// </remarks>
    SameTarget = 1 << 2,

    /// <summary>
    ///     Requires that the <see cref="Event"/> types match in order to be considered a duplicate.
    /// </summary>
    /// <remarks>
    ///     If your DoAfter should block other unrelated DoAfters involving the same set of entities, you may want
    ///     to disable this condition. E.g. force feeding a donk pocket and forcefully giving someone a donk pocket
    ///     should be mutually exclusive, even though the DoAfters have unrelated effects.
    /// </remarks>
    SameEvent = 1 << 3,

    All = SameTool | SameTarget | SameEvent,
}

public enum AttemptFrequency : byte
{
    /// <summary>
    ///     Never raise the attempt event.
    /// </summary>
    Never = 0,

    /// <summary>
    ///     Raises the attempt event when the DoAfter is about to start or end.
    /// </summary>
    StartAndEnd = 1,

    /// <summary>
    ///     Raise the attempt event every tick while the DoAfter is running.
    /// </summary>
    EveryTick = 2
}
