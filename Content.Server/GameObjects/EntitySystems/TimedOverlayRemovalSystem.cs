using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class TimedOverlayRemovalSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ServerOverlayEffectsComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                var effectsComponent = entity.GetComponent<ServerOverlayEffectsComponent>();
                foreach (var overlay in effectsComponent.ActiveOverlays.ToArray())
                {
                    if (overlay.TryGetOverlayParameter<TimedOverlayParameter>(out var parameter))
                    {
                        if (parameter.StartedAt + parameter.Length <= _gameTiming.CurTime.TotalMilliseconds)
                        {
                            effectsComponent.RemoveOverlay(overlay);
                        }
                    }
                }
            }
        }
    }
}
