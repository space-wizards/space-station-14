using Content.Server.GameObjects.EntitySystems.AI.LoadBalancer;
using Content.Shared.AI;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.AI
{
#if DEBUG
    [UsedImplicitly]
    public class ServerAiDebugSystem : EntitySystem
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
