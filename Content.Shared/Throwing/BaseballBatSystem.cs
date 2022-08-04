using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Throwing
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class BaseballBatSystem : EntitySystem
    {
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BaseballBatComponent, WideAttackEvent>(OnBatAttack);
        }

        public void OnBatAttack(EntityUid uid, BaseballBatComponent component, WideAttackEvent args)
        {

            var location = EntityManager.GetComponent<TransformComponent>(args.User).Coordinates;
            var diff = args.ClickLocation.ToMapPos(EntityManager) - location.ToMapPos(EntityManager);
            var angle = Angle.FromWorldVec(diff);

            //if (!EntityManager.TryGetComponent<ThrownItemComponent>(args.User, out var thrownItemComponent))
            //    return;

            //EntityManager.RemoveComponent<ThrownItemComponent>(args.User);

            var dir = (args.ClickLocation.ToMapPos(EntityManager) - EntityManager.GetComponent<TransformComponent>(args.User).WorldPosition).Normalized;

            foreach (var entity in _entityLookupSystem.GetEntitiesInArc(location, 7.0f, dir, 45f))
            {
                _throwingSystem.TryThrow(entity, dir * 20f, 10f, uid);
            }
        }
    }
}
