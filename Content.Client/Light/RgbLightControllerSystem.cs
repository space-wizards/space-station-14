using System;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Light
{
    public class RgbLightControllerSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var curTime = _gameTiming.CurTime;
            foreach (var (rgb, light, sprite) in EntityManager.EntityQuery<RgbLightControllerComponent, PointLightComponent, SpriteComponent>())
            {
                light.Color = GetCurrentRgbColor(curTime, TimeSpan.FromSeconds(rgb.CreationTick.Value * _gameTiming.TickPeriod.TotalSeconds), rgb);
                sprite.Color = GetCurrentRgbColor(curTime, TimeSpan.FromSeconds(rgb.CreationTick.Value * _gameTiming.TickPeriod.TotalSeconds), rgb);
            }
        }

        public static Color GetCurrentRgbColor(TimeSpan curTime, TimeSpan offset, RgbLightControllerComponent rgb)
        {
            return Color.FromHsv(new Vector4(
                (float) (((curTime.TotalSeconds - offset.TotalSeconds) / rgb.CycleRate + Math.Abs(rgb.Owner.GetHashCode() * 0.1)) % 1),
                1.0f,
                1.0f,
                1.0f
            ));
        }
    }
}
