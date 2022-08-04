using System.Linq;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Throwing
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class BaseballBatSystem : EntitySystem
    {
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

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


            //if (!EntityManager.TryGetComponent<ThrownItemComponent>(args.User, out var thrownItemComponent))
            //    return;

            //EntityManager.RemoveComponent<ThrownItemComponent>(args.User);

            foreach (var entity in _entityLookupSystem.GetEntitiesInArc(location, 0.5f, diff.ToAngle(), 50f, LookupFlags.None))
            {
                if (EntityManager.HasComponent<ItemComponent>(entity))
                {
                    var dir = args.ClickLocation.ToMapPos(EntityManager) - EntityManager.GetComponent<TransformComponent>(args.User).WorldPosition.Normalized;
                    _throwingSystem.TryThrow(entity, dir * _random.NextFloat(2f, 6f), 2f * component.WackForceMultiplier, uid);
                    if (EntityManager.HasComponent<ThrownItemComponent>(entity))
                        _audioSystem.Play("/Audio/Effects/hit_kick.ogg", Filter.Pvs(args.User), entity, AudioParams.Default);
                }

            }
        }
    }
}
