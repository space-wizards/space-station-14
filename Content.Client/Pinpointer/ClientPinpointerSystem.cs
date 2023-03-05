using Content.Shared.Pinpointer;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace Content.Client.Pinpointer
{
    public sealed class ClientPinpointerSystem : SharedPinpointerSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PinpointerComponent, ComponentHandleState>(HandleCompState);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            // we want to show pinpointers arrow direction relative
            // to players eye rotation (like it was in SS13)

            // because eye can change it rotation anytime
            // we need to update this arrow in a update loop
            foreach (var pinpointer in EntityQuery<PinpointerComponent>())
            {
                UpdateAppearance(pinpointer.Owner, pinpointer);
                UpdateEyeDir(pinpointer.Owner, pinpointer);
            }
        }

        private void HandleCompState(EntityUid uid, PinpointerComponent pinpointer, ref ComponentHandleState args)
        {
            if (args.Current is not PinpointerComponentState state)
                return;

            pinpointer.IsActive = state.IsActive;
            pinpointer.ArrowAngle = state.ArrowAngle;
            pinpointer.DistanceToTarget = state.DistanceToTarget;
        }

        private void UpdateAppearance(EntityUid uid, PinpointerComponent? pinpointer = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pinpointer, ref appearance))
                return;

            _appearance.SetData(uid, PinpointerVisuals.IsActive, pinpointer.IsActive, appearance);
            _appearance.SetData(uid, PinpointerVisuals.TargetDistance, pinpointer.DistanceToTarget, appearance);
        }

        private void UpdateArrowAngle(EntityUid uid, Angle angle, PinpointerComponent? pinpointer = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pinpointer, ref appearance))
                return;

            _appearance.SetData(uid, PinpointerVisuals.ArrowAngle, angle, appearance);
        }

        /// <summary>
        ///     Transform pinpointer arrow from world space to eye space
        ///     And send it to the appearance component
        /// </summary>
        private void UpdateEyeDir(EntityUid uid, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer) || !pinpointer.HasTarget)
                return;

            var eye = _eyeManager.CurrentEye;
            var angle = pinpointer.ArrowAngle + eye.Rotation;
            UpdateArrowAngle(uid, angle, pinpointer);
        }
    }
}
