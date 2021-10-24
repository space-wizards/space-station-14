using System;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.Light
{
    public class RgbLightControllerSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (rgb, sprite) in EntityManager.EntityQuery<SharedRgbLightControllerComponent, SpriteComponent>())
            {
                sprite.Color = SharedRgbLightControllerSystem.GetCurrentRgbColor(_gameTiming, TimeSpan.FromSeconds(rgb.CreationTick.Value * _gameTiming.TickPeriod.TotalSeconds), rgb);
            }
        }
    }
}
