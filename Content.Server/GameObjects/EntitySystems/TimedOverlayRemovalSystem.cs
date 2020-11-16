using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class TimedOverlayRemovalSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<ServerOverlayEffectsComponent>())
            {
                
                foreach (var overlay in component.ActiveOverlays.ToArray())
                {
                    if (overlay.TryGetOverlayParameter<TimedOverlayParameter>(out var parameter))
                    {
                        if (parameter.StartedAt + parameter.Length <= _gameTiming.CurTime.TotalMilliseconds)
                        {
                            component.RemoveOverlay(overlay);
                        }
                    }
                }
            }
        }
    }
}
