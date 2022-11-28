using System.Threading;
using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Server.DoAfter
{
    public sealed class DoAfterEventArgs
    {
        /// <summary>
        ///     The entity invoking do_after
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     How long does the do_after require to complete
        /// </summary>
        public float Delay { get; }

        /// <summary>
        ///     Applicable target (if relevant)
        /// </summary>
        public EntityUid? Target { get; }

        /// <summary>
        ///     Entity used by the User on the Target.
        /// </summary>
        public EntityUid? Used { get; set; }

        /// <summary>
        ///     Manually cancel the do_after so it no longer runs
        /// </summary>
        public CancellationToken CancelToken { get; }

        // Break the chains
        /// <summary>
        ///     Whether we need to keep our active hand as is (i.e. can't change hand or change item).
        ///     This also covers requiring the hand to be free (if applicable).
        /// </summary>
        public bool NeedHand { get; set; }

        /// <summary>
        ///     If do_after stops when the user moves
        /// </summary>
        public bool BreakOnUserMove { get; set; }

        /// <summary>
        ///     If do_after stops when the target moves (if there is a target)
        /// </summary>
        public bool BreakOnTargetMove { get; set; }

        /// <summary>
        ///     Threshold for user and target movement
        /// </summary>
        public float MovementThreshold { get; set; }

        public bool BreakOnDamage { get; set; }

        /// <summary>
        ///     Threshold for user damage
        /// </summary>
        public FixedPoint2 DamageThreshold { get; set; }
        public bool BreakOnStun { get; set; }

        /// <summary>
        ///     Threshold for distance user from the used OR target entities.
        /// </summary>
        public float? DistanceThreshold { get; set; }

        /// <summary>
        ///     Requires a function call once at the end (like InRangeUnobstructed).
        /// </summary>
        /// <remarks>
        ///     Anything that needs a pre-check should do it itself so no DoAfterState is ever sent to the client.
        /// </remarks>
        public Func<bool>? PostCheck { get; set; } = null;

        /// <summary>
        ///     Additional conditions that need to be met. Return false to cancel.
        /// </summary>
        public Func<bool>? ExtraCheck { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="User"/> entity when the DoAfter is cancelled.
        /// </summary>
        public object? UserCancelledEvent { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="User"/> entity when the DoAfter is finished successfully.
        /// </summary>
        public object? UserFinishedEvent { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="Used"/> entity when the DoAfter is cancelled.
        /// </summary>
        public object? UsedCancelledEvent { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="Used"/> entity when the DoAfter is finished successfully.
        /// </summary>
        public object? UsedFinishedEvent { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="Target"/> entity when the DoAfter is cancelled.
        /// </summary>
        public object? TargetCancelledEvent { get; set; }

        /// <summary>
        ///     Event to be raised directed to the <see cref="Target"/> entity when the DoAfter is finished successfully.
        /// </summary>
        public object? TargetFinishedEvent { get; set; }

        /// <summary>
        ///     Event to be broadcast when the DoAfter is cancelled.
        /// </summary>
        public object? BroadcastCancelledEvent { get; set; }

        /// <summary>
        ///     Event to be broadcast when the DoAfter is finished successfully.
        /// </summary>
        public object? BroadcastFinishedEvent { get; set; }

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
}
