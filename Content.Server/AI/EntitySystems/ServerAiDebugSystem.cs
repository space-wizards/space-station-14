using Content.Server.AI.LoadBalancer;
using Content.Shared.AI;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.EntitySystems
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
