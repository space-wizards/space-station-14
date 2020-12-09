using Content.Client.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AlertsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            foreach (var clientAlertsComponent in EntityManager.ComponentManager.EntityQuery<ClientAlertsComponent>())
            {
                clientAlertsComponent.FrameUpdate(frameTime);
            }
        }
    }
}
