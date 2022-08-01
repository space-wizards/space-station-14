using Content.Server.NPC.LoadBalancer;
using Content.Shared.AI;
using JetBrains.Annotations;

namespace Content.Server.NPC.Systems
{
#if DEBUG
    [UsedImplicitly]
    public sealed class ServerAiDebugSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            AiActionRequestJob.FoundAction += NotifyActionJob;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            AiActionRequestJob.FoundAction -= NotifyActionJob;
        }

        private void NotifyActionJob(SharedAiDebug.UtilityAiDebugMessage message)
        {
            RaiseNetworkEvent(message);
        }
    }
#endif
}
