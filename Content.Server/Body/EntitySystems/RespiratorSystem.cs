using System;
using Content.Server.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.MobState;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.EntitySystems
{
    public class RespiratorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RespiratorComponent, AddedToBodyEvent>(OnAddedToBody);
        }

        private void OnAddedToBody(EntityUid uid, RespiratorComponent component, AddedToBodyEvent args)
        {
            component.Inhale(component.CycleDelay);
        }

        public override void Update(float frameTime)
        {
            foreach (var respirator in EntityManager.EntityQuery<RespiratorComponent>(false))
            {
                // TODO MIRROR instead of explicitly checking for critical, just make lung behavior not run if the heart
                // isn't pumping blood, and have the heart stop pumping when you die (?)
                if (respirator.Body != null && respirator.Body.Owner.TryGetComponent(out IMobStateComponent? mobState) && mobState.IsCritical())
                {
                    return;
                }

                if (respirator.Status == LungStatus.None)
                {
                    respirator.Status = LungStatus.Inhaling;
                }

                respirator.AccumulatedFrametime += respirator.Status switch
                {
                    LungStatus.Inhaling => frameTime,
                    LungStatus.Exhaling => -frameTime,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var absoluteTime = Math.Abs(respirator.AccumulatedFrametime);
                var delay = respirator.CycleDelay;

                if (absoluteTime < delay)
                {
                    return;
                }

                switch (respirator.Status)
                {
                    case LungStatus.Inhaling:
                        respirator.Inhale(absoluteTime);
                        respirator.Status = LungStatus.Exhaling;
                        break;
                    case LungStatus.Exhaling:
                        respirator.Exhale(absoluteTime);
                        respirator.Status = LungStatus.Inhaling;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                respirator.AccumulatedFrametime = absoluteTime - delay;
            }
        }
    }
}
