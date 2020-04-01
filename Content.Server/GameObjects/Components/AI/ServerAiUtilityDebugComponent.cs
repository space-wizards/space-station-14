using Content.Server.GameObjects.EntitySystems.AI.LoadBalancer;
using Content.Shared.GameObjects.Components.AI;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.AI
{
    [RegisterComponent]
    public class ServerAiUtilityDebugComponent : SharedAiDebugComponent
    {
        public override void Initialize()
        {
            base.Initialize();
            AiActionRequestJob.FoundAction += plan =>
            {
                SendNetworkMessage(plan);
            };
        }
    }
}
