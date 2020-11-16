using System;
using Content.Server.GameObjects.EntitySystems.AI.Steering;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToEntityOperator : AiOperator
    {
        // TODO: This and steering need to support InRangeUnobstructed now
        private readonly IEntity _owner;
        private EntityTargetSteeringRequest _request;
        private readonly IEntity _target;
        // For now we'll just get as close as we can because we're not doing LOS checks to be able to pick up at the max interaction range
        public float ArrivalDistance { get; }
        public float PathfindingProximity { get; }

        private bool _requiresInRangeUnobstructed;

        public MoveToEntityOperator(
            IEntity owner, 
            IEntity target, 
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

        public override bool TryStartup()
        {
            if (!base.TryStartup())
            {
                return true;
            }

            var steering = EntitySystem.Get<AiSteeringSystem>();
            _request = new EntityTargetSteeringRequest(_target, ArrivalDistance, PathfindingProximity, _requiresInRangeUnobstructed);
            steering.Register(_owner, _request);
            return true;
        }
        
        public override void Shutdown(Outcome outcome)
        {
            base.Shutdown(outcome);
            var steering = EntitySystem.Get<AiSteeringSystem>();
            steering.Unregister(_owner);
        }

        public override Outcome Execute(float frameTime)
        {
            switch (_request.Status)
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