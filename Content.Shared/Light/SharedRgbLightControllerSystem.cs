using System;
using Content.Shared.Light.Component;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Shared.Light
{
    public class SharedRgbLightControllerSystem : EntitySystem
    {
        [Dependency] private IGameTiming _gameTiming = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (rgb, light) in EntityManager.EntityQuery<SharedRgbLightControllerComponent, SharedPointLightComponent>())
            {
                light.Color = GetCurrentRgbColor(_gameTiming, TimeSpan.FromSeconds(rgb.CreationTick.Value * _gameTiming.TickPeriod.TotalSeconds), rgb);
            }
        }

        public static Color GetCurrentRgbColor(IGameTiming gameTiming, TimeSpan offset, SharedRgbLightControllerComponent rgb)
        {
            return Color.FromHsv(new Vector4(
                (float) (((gameTiming.CurTime.TotalSeconds - offset.TotalSeconds) / rgb.CycleRate + Math.Abs(rgb.Owner.Uid.GetHashCode() * 0.1)) % 1),
                1.0f,
                1.0f,
                1.0f
            ));
        }
    }
}
