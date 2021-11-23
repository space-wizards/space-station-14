using Content.Server.Nutrition.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Nutrition.EntitySystems
{
    public class ForcefeedOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly FoodSystem _foodSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForcefeedOnCollideComponent, ThrowDoHitEvent>(OnThrowDoHit);
            SubscribeLocalEvent<ForcefeedOnCollideComponent, LandEvent>(OnLand);
        }

        private void OnThrowDoHit(EntityUid uid, ForcefeedOnCollideComponent component, ThrowDoHitEvent args)
        {
            if (!args.Target.HasComponent<HungerComponent>())
                return;
            if (!EntityManager.TryGetComponent<FoodComponent>(uid, out var food))
                return;

            // the 'target' isnt really the 'user' per se.. but..
            _foodSystem.TryUseFood(food.OwnerUid, args.Target.Uid, args.Target.Uid);
        }

        private void OnLand(EntityUid uid, ForcefeedOnCollideComponent component, LandEvent args)
        {
            if (!component.RemoveOnThrowEnd)
                return;

            EntityManager.RemoveComponent(uid, component);
        }
    }
}
