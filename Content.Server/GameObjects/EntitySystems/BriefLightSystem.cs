using Content.Server.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Used to attach PointLights to entities that only last a short-time while going transparent
    /// </summary>
    public class BriefLightSystem : EntitySystem
    {
        private IGameTiming _gameTiming;

        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(BriefLightComponent));
            _gameTiming = IoCManager.Resolve<IGameTiming>();
        }

        public static void BriefLightHelper(IEntity entity, double duration)
        {
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
            if (!entity.TryGetComponent(out BriefLightComponent briefLightComponent))
            {
                var briefLight = entity.AddComponent<BriefLightComponent>();
                briefLight.StartTime = currentTime;
                briefLight.Duration = duration;
                return;
            }

            var newEndTime = currentTime.TotalSeconds + duration;

            if (briefLightComponent.EndTime.TotalSeconds > newEndTime)
            {
                return;
            }

            briefLightComponent.StartTime = currentTime;
            briefLightComponent.Duration = duration;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in RelevantEntities)
            {
                var briefLight = entity.GetComponent<BriefLightComponent>();
                if (briefLight.EndTime > _gameTiming.CurTime)
                {
                    entity.RemoveComponent<BriefLightComponent>();
                    continue;
                }

                var elapsedTime = _gameTiming.CurTime - briefLight.StartTime;

                briefLight.SetAlpha(elapsedTime.TotalSeconds / briefLight.Duration);
            }
        }
    }
}
