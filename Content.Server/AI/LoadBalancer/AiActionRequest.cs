using Content.Server.AI.Utility.Actions;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.LoadBalancer
{
    public sealed class AiActionRequest
    {
        public EntityUid EntityUid { get; }
        public Blackboard? Context { get; }
        public IEnumerable<IAiUtility>? Actions { get; }

        public AiActionRequest(EntityUid uid, Blackboard context, IEnumerable<IAiUtility> actions)
        {
            EntityUid = uid;
            Context = context;
            Actions = actions;
        }
    }
}
