using Content.Server.Nutrition.Components;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed class ForcefeedOnCollideSystem : EntitySystem
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
            _foodSystem.ProjectileForceFeed(uid, args.Target, args.User);
        }

        private void OnLand(EntityUid uid, ForcefeedOnCollideComponent component, LandEvent args)
        {
            if (!component.RemoveOnThrowEnd)
                return;

            EntityManager.RemoveComponent(uid, component);
        }
    }
}
