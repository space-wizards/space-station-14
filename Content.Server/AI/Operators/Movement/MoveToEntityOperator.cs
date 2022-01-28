using System;
using Content.Server.AI.Steering;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToEntityOperator : AiOperator
    {
        // TODO: This and steering need to support InRangeUnobstructed now
        private readonly EntityUid _owner;
        private EntityTargetSteeringRequest? _request;
        private readonly EntityUid _target;
        // For now we'll just get as close as we can because we're not doing LOS checks to be able to pick up at the max interaction range
        public float ArrivalDistance { get; }
        public float PathfindingProximity { get; }

        private readonly bool _requiresInRangeUnobstructed;

        public MoveToEntityOperator(
            EntityUid owner,
            EntityUid target,
            float arrivalDistance = 1.0f,
            float pathfindingProximity = 1.5f,
            bool requiresInRangeUnobstructed = false)
        {
            _owner = owner;
            _target = target;
            ArrivalDistance = arrivalDistance;
            PathfindingProximity = pathfindingProximity;
            _requiresInRangeUnobstructed = requiresInRangeUnobstructed;
        }

        public override bool Startup()
        {
            if (!base.Startup())
            {
                return true;
            }

            var steering = EntitySystem.Get<AiSteeringSystem>();
            _request = new EntityTargetSteeringRequest(_target, ArrivalDistance, PathfindingProximity, _requiresInRangeUnobstructed);
            steering.Register(_owner, _request);
            return true;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            var steering = EntitySystem.Get<AiSteeringSystem>();
            steering.Unregister(_owner);
            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            switch (_request?.Status)
            {
                case SteeringStatus.Pending:
                    DebugTools.Assert(EntitySystem.Get<AiSteeringSystem>().IsRegistered(_owner));
                    return Outcome.Continuing;
                case SteeringStatus.NoPath:
                    return Outcome.Failed;
                case SteeringStatus.Arrived:
                    return Outcome.Success;
                case SteeringStatus.Moving:
                    DebugTools.Assert(EntitySystem.Get<AiSteeringSystem>().IsRegistered(_owner));
                    return Outcome.Continuing;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
