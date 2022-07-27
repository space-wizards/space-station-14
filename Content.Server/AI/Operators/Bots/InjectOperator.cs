using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Operators.Bots
{
    public sealed class InjectOperator : AiOperator
    {
        private EntityUid _medibot;
        private EntityUid _target;
        public InjectOperator(EntityUid medibot, EntityUid target)
        {
            _medibot = medibot;
            _target = target;
        }

        public override Outcome Execute(float frameTime)
        {
            var injectSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InjectNearbySystem>();
            if (injectSystem.Inject(_medibot, _target))
                return Outcome.Success;

            return Outcome.Failed;
        }
    }
}
