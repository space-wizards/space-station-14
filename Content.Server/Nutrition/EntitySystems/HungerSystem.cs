using Content.Server.Nutrition.Components;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;

namespace Content.Server.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class HungerSystem : EntitySystem
    {
        private float _accumulatedFrameTime;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HungerComponent, RejuvenateEvent>(OnRejuvenate);
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                foreach (var comp in EntityManager.EntityQuery<HungerComponent>())
                {
                    comp.OnUpdate(_accumulatedFrameTime);
                }

                _accumulatedFrameTime -= 1;
            }
        }

        private void OnRejuvenate(EntityUid uid, HungerComponent component, RejuvenateEvent args)
        {
            component.ResetFood();
        }
    }
}
