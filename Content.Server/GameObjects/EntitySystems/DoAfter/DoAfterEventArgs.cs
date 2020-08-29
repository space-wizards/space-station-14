#nullable enable
using System;
using System.Threading;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

// ReSharper disable UnassignedReadonlyField

namespace Content.Server.GameObjects.EntitySystems.DoAfter
{
    public sealed class DoAfterEventArgs
    {
        // Premade checks
        public Func<bool> GetInRangeUnobstructed(int collisionMask = (int) CollisionGroup.MobMask)
        {
            if (Target == null)
            {
                throw new InvalidOperationException("Can't supply a null target to DoAfterEventArgs.GetInRangeUnobstructed");
            }
            var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
            Func<IEntity, bool> ignored = entity => entity == User || entity == Target;
            return () => interactionSystem.InRangeUnobstructed(User.Transform.MapPosition, Target.Transform.MapPosition, collisionMask: collisionMask, predicate: ignored);
        }
        
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
        public bool NeedHand { get; set; }

        /// <summary>
        ///     If do_after stops when the user moves
        /// </summary>
        public bool BreakOnUserMove { get; set; }

        /// <summary>
        ///     If do_after stops when the target moves (if there is a target)
        /// </summary>
        public bool BreakOnTargetMove { get; set; }

        public bool BreakOnDamage { get; set; }
        public bool BreakOnStun { get; set; }

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
