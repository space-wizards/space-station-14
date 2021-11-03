using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.Pinpointer
{
    public abstract class SharedPinpointerSystem : EntitySystem
    {
        protected readonly HashSet<EntityUid> ActivePinpointers = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PinpointerComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<PinpointerComponent, ComponentShutdown>(OnPinpointerShutdown);
        }

        private void GetCompState(EntityUid uid, PinpointerComponent pinpointer, ref ComponentGetState args)
        {
            args.State = new PinpointerComponentState
            {
                IsActive = pinpointer.IsActive,
                DirectionToTarget = pinpointer.DirectionToTarget,
                DistanceToTarget = pinpointer.DistanceToTarget
            };
        }

        private void OnPinpointerShutdown(EntityUid uid, PinpointerComponent component, ComponentShutdown _)
        {
            // no need to dirty it/etc: it's shutting down anyway!
            ActivePinpointers.Remove(uid);
        }

        /// <summary>
        ///     Manually set distance from pinpointer to target
        /// </summary>
        public void SetDistance(EntityUid uid, Distance distance, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            if (distance == pinpointer.DistanceToTarget)
                return;

            pinpointer.DistanceToTarget = distance;
            pinpointer.Dirty();
        }

        /// <summary>
        ///     Manually set pinpointer arrow direction
        /// </summary>
        public void SetDirection(EntityUid uid, Direction directionToTarget, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            if (directionToTarget == pinpointer.DirectionToTarget)
                return;

            pinpointer.DirectionToTarget = directionToTarget;
            pinpointer.Dirty();
        }

        /// <summary>
        ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
        /// </summary>
        public void SetActive(EntityUid uid, bool isActive, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;
            if (isActive == pinpointer.IsActive)
                return;

            // add-remove pinpointer from update list
            if (isActive)
                ActivePinpointers.Add(uid);
            else
                ActivePinpointers.Remove(uid);

            pinpointer.IsActive = isActive;
            pinpointer.Dirty();
        }


        /// <summary>
        ///     Toggle Pinpointer screen. If it has target it will start tracking it.
        /// </summary>
        /// <returns>True if pinpointer was activated, false otherwise</returns>
        public bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return false;

            var isActive = !pinpointer.IsActive;
            SetActive(uid, isActive, pinpointer);
            return isActive;
        }
    }
}
