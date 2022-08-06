using Content.Server.AI.Steering;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.AI.Operators.Movement
{
    public sealed class MoveToGridOperator : AiOperator
    {
        private readonly EntityUid _owner;
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

            var steering = EntitySystem.Get<NPCSteeringSystem>();
            var comp = steering.Register(_owner, _target);
            comp.Range = DesiredRange;
            return true;
        }

        public override bool Shutdown(Outcome outcome)
        {
            if (!base.Shutdown(outcome))
                return false;

            var steering = EntitySystem.Get<NPCSteeringSystem>();
            steering.Unregister(_owner);
            return true;
        }

        public override Outcome Execute(float frameTime)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<NPCSteeringComponent>(_owner, out var steering))
                return Outcome.Failed;

            switch (steering.Status)
            {
                case SteeringStatus.NoPath:
                    return Outcome.Failed;
                case SteeringStatus.InRange:
                    return Outcome.Success;
                case SteeringStatus.Moving:
                    return Outcome.Continuing;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
