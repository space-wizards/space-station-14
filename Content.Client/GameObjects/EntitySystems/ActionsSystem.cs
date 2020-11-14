using Content.Client.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public class ActionsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            foreach (var actionsComponent in EntityManager.ComponentManager.EntityQuery<ClientActionsComponent>())
            {
                actionsComponent.FrameUpdate(frameTime);
            }
        }
    }
}
