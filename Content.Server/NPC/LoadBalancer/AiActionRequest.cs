using Content.Server.NPC.Utility.Actions;
using Content.Server.NPC.WorldState;

namespace Content.Server.NPC.LoadBalancer
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
