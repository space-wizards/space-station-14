#nullable enable
using System;
using System.Threading;
using Robust.Shared.Interfaces.GameObjects;
// ReSharper disable UnassignedReadonlyField

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class DoAfterEventArgs
    {
        /// <summary>
        ///     The entity invoking do_after
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     How long does the do_after require to complete
        /// </summary>
        public float Delay { get; }

        /// <summary>
        ///     Applicable target (if relevant)
        /// </summary>
        public IEntity? Target { get; }

        /// <summary>
        ///     Manually cancel the do_after so it no longer runs
        /// </summary>
        public CancellationToken CancelToken { get; }

        // Break the chains
        /// <summary>
        ///     Whether we need to keep our active hand as is (i.e. can't change hand or change item).
        ///     This also covers requiring the hand to be free (if applicable).
        /// </summary>
        public readonly bool NeedHand;

        /// <summary>
        ///     If do_after stops when the user moves
        /// </summary>
        public readonly bool BreakOnUserMove;

        /// <summary>
        ///     If do_after stops when the target moves (if there is a target)
        /// </summary>
        public readonly bool BreakOnTargetMove;

        public readonly bool BreakOnDamage;
        public readonly bool BreakOnStun;

        /// <summary>
        ///     Additional conditions that need to be met. Return false to cancel.
        /// </summary>
        public readonly Func<bool>? ExtraCheck;

        public DoAfterEventArgs(
            IEntity user,
            float delay,
            CancellationToken cancelToken = default,
            IEntity? target = null)
        {
            User = user;
            Delay = delay;
            CancelToken = cancelToken;
            Target = target;

            if (Target == null)
            {
                BreakOnTargetMove = false;
            }
        }
    }
}
