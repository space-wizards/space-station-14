using Content.Shared.Solar.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public sealed class PowerSolarSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SolarPanelComponent, ComponentHandleState>(HandleSolarPanelState);
        }

        private void HandleSolarPanelState(EntityUid uid, SolarPanelComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not SolarPanelComponentState state) return;
            component.StartAngle = state.Angle;
            component.AngularVelocity = state.AngularVelocity;
            component.LastUpdate = state.LastUpdate;
        }

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
