using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Pinpointer
{
    public class PinpointerSystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        private readonly HashSet<EntityUid> _activePinpointers = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PinpointerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, PinpointerComponent component, UseInHandEvent args)
        {
            TogglePinpointer(uid, component);

            // check automatically find target
            if (component.IsActive && component.Whitelist != null)
            {
                var target = FindTargetFromWhitelist(uid, component.Whitelist);
                SetTarget(uid, target, component);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var uid in _activePinpointers)
            {
                if (!EntityManager.TryGetComponent(uid, out PinpointerComponent? pinpointer))
                    continue;

                var target = pinpointer.Target;
                if (target != null && EntityManager.EntityExists(target.Value))
                {
                    var dir = CalculateDirection(uid, target.Value);
                    pinpointer.DirectionToTarget = dir;
                }
                else
                {
                    pinpointer.DirectionToTarget = Direction.Invalid;
                }

                UpdateAppearance(uid, pinpointer);
            }    
        }

        /// <summary>
        ///     Calculate direction to pinpointers target
        /// </summary>
        private Direction CalculateDirection(EntityUid pinUid, EntityUid trgUid)
        {
            // check if entities have transform component
            if (!EntityManager.TryGetComponent(pinUid, out ITransformComponent? pin))
                return Direction.Invalid;
            if (!EntityManager.TryGetComponent(trgUid, out ITransformComponent? trg))
                return Direction.Invalid;

            // check if they are on same map
            if (pin.MapID != trg.MapID)
                return Direction.Invalid;

            // get world direction vector
            var dir = (trg.WorldPosition - pin.WorldPosition).GetDir();
            return dir;
        }

        /// <summary>
        ///     Try to find the closest entity from whitelist
        ///     Will return null if can't find anything
        /// </summary>
        private EntityUid? FindTargetFromWhitelist(EntityUid uid, EntityWhitelist whitelist,
            ITransformComponent? transform = null)
        {
            if (!Resolve(uid, ref transform))
                return null;

            var mapId = transform.MapID;
            var ents = _entityLookup.GetEntitiesInMap(mapId);

            // sort all entities in distance increasing order
            var l = new SortedList<float, EntityUid>();
            foreach (var e in ents)
            {
                if (whitelist.IsValid(e))
                {
                    var dist = (e.Transform.WorldPosition - transform.WorldPosition).LengthSquared;
                    l.TryAdd(dist, e.Uid);
                }
            }

            // return uid with a smallest distacne
            return l.Count > 0 ? l.First().Value : null;
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
            UpdateAppearance(uid, pinpointer);
        }

        private void UpdateAppearance(EntityUid uid, PinpointerComponent? pinpointer = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pinpointer, ref appearance))
                return;

            appearance.SetData(PinpointerVisuals.IsActive, pinpointer.IsActive);
            appearance.SetData(PinpointerVisuals.TargetDirection, (sbyte) pinpointer.DirectionToTarget);
        }
    }
}
