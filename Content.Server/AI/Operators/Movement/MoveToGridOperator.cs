using System;
using Content.Server.AI.Steering;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToGridOperator : AiOperator
    {
        private readonly EntityUid _owner;
        private GridTargetSteeringRequest? _request;
        private readonly EntityCoordinates _target;
        public float DesiredRange { get; set; }

        public MoveToGridOperator(EntityUid owner, EntityCoordinates target, float desiredRange = 1.5f)
        {
            _owner = owner;
            _target = target;
            DesiredRange = desiredRange;
        }

        public override bool Startup()
        {
            if (!base.Startup())
            {
                return true;
            }

            var steering = EntitySystem.Get<AiSteeringSystem>();
            _request = new GridTargetSteeringRequest(_target, DesiredRange);
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
