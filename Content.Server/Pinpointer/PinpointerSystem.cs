using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.Pinpointer
{
    public class PinpointerSystem : EntitySystem
    {
        private readonly HashSet<EntityUid> _activePinpointers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PinpointerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, PinpointerComponent component, UseInHandEvent args)
        {
            TogglePinpointer(uid, component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var uid in _activePinpointers)
            {
                if (!EntityManager.TryGetComponent(uid, out PinpointerComponent? pinpointer))
                    continue;

                if (pinpointer.Target == null)
                    continue;

                var dir = CalculateDirection(uid, pinpointer.Target.Value);
            }    
        }

        private Direction CalculateDirection(EntityUid fromUid, EntityUid toUid)
        {
            // check if entities have transform component
            if (!EntityManager.TryGetComponent(fromUid, out ITransformComponent? from))
                return Direction.Invalid;
            if (!EntityManager.TryGetComponent(toUid, out ITransformComponent? to))
                return Direction.Invalid;

            // check if they are on same map
            if (from.MapID != to.MapID)
                return Direction.Invalid;

            var dir = (to.WorldPosition - from.WorldPosition).GetDir();
            return dir;
        }

        /// <summary>
        ///     Set pinpointers target to track
        /// </summary>
        public void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            pinpointer.Target = target;
        }

        /// <summary>
        ///     Toggle Pinpointer screen. If it has target it will start tracking it.
        /// </summary>
        public void TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            pinpointer.IsActive = !pinpointer.IsActive;

            // add-remove pinpointer from update list
            if (pinpointer.IsActive)
                _activePinpointers.Add(uid);
            else
                _activePinpointers.Remove(uid);

            // update pinpointer appearance
            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(PinpointerVisuals.IsActive, pinpointer.IsActive);
            }
        }
    }
}
