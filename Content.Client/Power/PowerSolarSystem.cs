using Content.Shared.Solar;
using Content.Shared.Solar.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power;

[UsedImplicitly]
public sealed class PowerSolarSystem : SharedPowerSolarSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQuery<SolarPanelComponent, SpriteComponent, TransformComponent>();
        foreach (var (panel, sprite, xform) in query)
        {
            Angle targetAngle = panel.StartAngle + panel.AngularVelocity * (GameTiming.CurTime - panel.LastUpdate).TotalSeconds * (Paused(panel.Owner) ? 0f : 1f);
            panel.Angle = targetAngle.Reduced();
            sprite.Rotation = panel.Angle - xform.LocalRotation;
        }
    }
}
