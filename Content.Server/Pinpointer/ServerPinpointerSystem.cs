using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Pinpointer
{
    public sealed class ServerPinpointerSystem : SharedPinpointerSystem
    {
        [Dependency] private readonly IEntityLookup _entityLookup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PinpointerComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid uid, PinpointerComponent component, UseInHandEvent args)
        {
            TogglePinpointer(uid, component);

            // try to find target from whitelist
            if (component.IsActive && component.Whitelist != null)
            {
                var target = FindTargetFromWhitelist(uid, component.Whitelist);
                SetTarget(uid, target, component);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // because target or pinpointer can move
            // we need to update pinpointers arrow each frame
            foreach (var uid in ActivePinpointers)
            {
                UpdateDirectionToTarget(uid);
            }    
        }

        /// <summary>
        ///     Try to find the closest entity from whitelist on a current map
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

            if (pinpointer.Target == target)
                return;

            pinpointer.Target = target;
            if (pinpointer.IsActive)
                UpdateDirectionToTarget(uid, pinpointer);
        }

        /// <summary>
        ///     Update direction from pinpointer to selected target (if it was set)
        /// </summary>
        private void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return;

            var target = pinpointer.Target;
            if (target == null || !EntityManager.EntityExists(target.Value))
            {
                SetDirection(uid, Direction.Invalid, pinpointer);
                SetDistance(uid, Distance.UNKNOWN, pinpointer);
                return;
            }

            var dirVec = CalculateDirection(uid, target.Value);
            if (dirVec != null)
            {
                var dir = dirVec.Value.GetDir();
                SetDirection(uid, dir, pinpointer);
                var dist = CalculateDistance(uid, dirVec.Value, pinpointer);
                SetDistance(uid, dist, pinpointer);
            }
            else
            {
                SetDirection(uid, Direction.Invalid, pinpointer);
                SetDistance(uid, Distance.UNKNOWN, pinpointer);
            }
        }

        /// <summary>
        ///     Calculate direction from pinUid to trgUid
        /// </summary>
        /// <returns>Null if failed to caluclate distance between two entities</returns>
        private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
        {
            // check if entities have transform component
            if (!EntityManager.TryGetComponent(pinUid, out ITransformComponent? pin))
                return null;
            if (!EntityManager.TryGetComponent(trgUid, out ITransformComponent? trg))
                return null;

            // check if they are on same map
            if (pin.MapID != trg.MapID)
                return null;

            // get world direction vector
            var dir = (trg.WorldPosition - pin.WorldPosition);
            return dir;
        }

        private Distance CalculateDistance(EntityUid uid, Vector2 vec, PinpointerComponent? pinpointer = null)
        {
            if (!Resolve(uid, ref pinpointer))
                return Distance.UNKNOWN;

            var dist = vec.Length;
            if (dist <= pinpointer.ReachedDistance)
                return Distance.REACHED;
            else if (dist <= pinpointer.CloseDistance)
                return Distance.CLOSE;
            else if (dist <= pinpointer.MediumDistance)
                return Distance.MEDIUM;
            else
                return Distance.FAR;
        }
    }
}
