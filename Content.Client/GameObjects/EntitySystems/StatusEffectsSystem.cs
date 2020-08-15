using Content.Client.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public class StatusEffectsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IGameTiming _gameTiming;
#pragma warning restore 649

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            foreach (var clientStatusEffectsComponent in EntityManager.ComponentManager.EntityQuery<ClientStatusEffectsComponent>())
            {
                clientStatusEffectsComponent.FrameUpdate(frameTime);
            }
        }
    }
}
