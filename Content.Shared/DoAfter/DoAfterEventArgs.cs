using System.Threading;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;
//TODO: Merge into DoAfter
[Serializable, NetSerializable]
public sealed class DoAfterEventArgs
{
    /// <summary>
    ///     The entity invoking do_after
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     How long does the do_after require to complete
    /// </summary>
    public float Delay;

    /// <summary>
    ///     Applicable target (if relevant)
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    ///     Entity used by the User on the Target.
    /// </summary>
    public EntityUid? Used;

    public bool RaiseOnUser = true;

    public bool RaiseOnTarget = true;

    public bool RaiseOnUsed = true;

    /// <summary>
    ///     Manually cancel the do_after so it no longer runs
    /// </summary>
    [NonSerialized]
    public CancellationToken CancelToken;

    // Break the chains
    /// <summary>
    ///     Whether we need to keep our active hand as is (i.e. can't change hand or change item).
    ///     This also covers requiring the hand to be free (if applicable).
    /// </summary>
    public bool NeedHand;

    /// <summary>
    ///     If do_after stops when the user moves
    /// </summary>
    public bool BreakOnUserMove;

    /// <summary>
    ///     If do_after stops when the target moves (if there is a target)
    /// </summary>
    public bool BreakOnTargetMove;

    /// <summary>
    ///     Threshold for user and target movement
    /// </summary>
    public float MovementThreshold;

    public bool BreakOnDamage;

    /// <summary>
    ///     Threshold for user damage
    /// </summary>
    public FixedPoint2? DamageThreshold;
    public bool BreakOnStun;

    /// <summary>
    /// Should the DoAfter event broadcast?
    /// </summary>
    public bool Broadcast;

    /// <summary>
    ///     Threshold for distance user from the used OR target entities.
    /// </summary>
    public float? DistanceThreshold;

    /// <summary>
    ///     Requires a function call once at the end (like InRangeUnobstructed).
    /// </summary>
    /// <remarks>
    ///     Anything that needs a pre-check should do it itself so no DoAfterState is ever sent to the client.
    /// </remarks>
    [NonSerialized]
    //TODO: Replace with eventbus
    public Func<bool>? PostCheck;

    /// <summary>
    ///     Additional conditions that need to be met. Return false to cancel.
    /// </summary>
    [NonSerialized]
    //TODO Replace with eventbus
    public Func<bool>? ExtraCheck;

    public DoAfterEventArgs(
        EntityUid user,
        float delay,
        CancellationToken cancelToken = default,
        EntityUid? target = null,
        EntityUid? used = null)
    {
        User = user;
        Delay = delay;
        CancelToken = cancelToken;
        Target = target;
        Used = used;
        MovementThreshold = 0.1f;
        DamageThreshold = 1.0;

        if (Target == null)
        {
            DebugTools.Assert(!BreakOnTargetMove);
            BreakOnTargetMove = false;
        }
    }
}
