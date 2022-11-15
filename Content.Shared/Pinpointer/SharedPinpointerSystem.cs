using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer
{
    public abstract class SharedPinpointerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PinpointerComponent, ComponentGetState>(GetCompState);
        }

        private void GetCompState(EntityUid uid, PinpointerComponent pinpointer, ref ComponentGetState args)
        {
            args.State = new PinpointerComponentState
            {
                IsActive = pinpointer.IsActive,
                ArrowAngle = pinpointer.ArrowAngle,
                DistanceToTarget = pinpointer.DistanceToTarget
            };
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
            Dirty(pinpointer);
        }

        /// <summary>
        ///     Try to manually set pinpointer arrow direction.
        ///     If difference between current angle and new angle is smaller than
        ///     pinpointer precision, new value will be ignored and it will return false.
        /// </summary>
        public bool TrySetArrowAngle(EntityUid uid, Angle arrowAngle, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return false;

            if (pinpointer.ArrowAngle.EqualsApprox(arrowAngle, pinpointer.Precision))
                return false;

            pinpointer.ArrowAngle = arrowAngle;
            Dirty(pinpointer);

            return true;
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
            
            pinpointer.IsActive = isActive;
            Dirty(pinpointer);
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
