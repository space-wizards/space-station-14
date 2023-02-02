using Content.Shared.Solar;
using Content.Shared.Solar.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public sealed class PowerSolarSystem : SharedPowerSolarSystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (panel, sprite, xform) in EntityManager.EntityQuery<SolarPanelComponent, SpriteComponent, TransformComponent>())
            {
                Angle a = panel.StartAngle + panel.AngularVelocity * (_gameTiming.CurTime - panel.LastUpdate).TotalSeconds;
                panel.Angle = a.Reduced();
                sprite.Rotation = panel.Angle - xform.LocalRotation;
            }
        }
    }
}
