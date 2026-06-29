using System.Numerics;
using Content.Server.Stunnable;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Hands.Systems
{
    public sealed partial class HandsSystem : SharedHandsSystem
    {
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private PullingSystem _pullingSystem = default!;
        [Dependency] private ThrowingSystem _throwingSystem = default!;
        [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

        /// <summary>
        /// Items dropped when the holder falls down will be launched in
        /// a direction offset by up to this many degrees from the holder's
        /// movement direction.
        /// </summary>
        private const float DropHeldItemsSpread = 45;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, DisarmedEvent>(OnDisarmed, before: new[] {typeof(StunSystem), typeof(SharedStaminaSystem)});

            SubscribeLocalEvent<HandsComponent, ComponentGetState>(GetComponentState);

            SubscribeLocalEvent<HandsComponent, BeforeExplodeEvent>(OnExploded);

            SubscribeLocalEvent<HandsComponent, DropHandItemsEvent>(OnDropHandItems);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HandsSystem>();
        }

        private void GetComponentState(EntityUid uid, HandsComponent hands, ref ComponentGetState args)
        {
            args.State = new HandsComponentState(hands);
        }


        private void OnExploded(Entity<HandsComponent> ent, ref BeforeExplodeEvent args)
        {
            if (ent.Comp.DisableExplosionRecursion)
                return;

            foreach (var held in EnumerateHeld(ent.AsNullable()))
            {
                args.Contents.Add(held);
            }
        }

        private void OnDisarmed(EntityUid uid, HandsComponent component, ref DisarmedEvent args)
        {
            if (args.Handled)
                return;

            // Break any pulls
            if (TryComp(uid, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
                _pullingSystem.TryStopPull(puller.Pulling.Value, pullable);

            var offsetRandomCoordinates = TransformSystem.GetMoverCoordinates(args.Target).Offset(_random.NextVector2(1f, 1.5f));
            if (!ThrowHeldItem(args.Target, offsetRandomCoordinates))
                return;

            args.PopupPrefix = "disarm-action-";

            args.Handled = true; // no shove/stun.
        }

        #region interactions
        private void OnDropHandItems(Entity<HandsComponent> entity, ref DropHandItemsEvent args)
        {
            // If the holder doesn't have a physics component, they ain't moving
            var holderVelocity = _physicsQuery.TryComp(entity, out var physics) ? physics.LinearVelocity : Vector2.Zero;
            var spreadMaxAngle = Angle.FromDegrees(DropHeldItemsSpread);

            foreach (var hand in entity.Comp.Hands.Keys)
            {
                if (!TryGetHeldItem(entity.AsNullable(), hand, out var heldEntity))
                    continue;

                var throwAttempt = new FellDownThrowAttemptEvent(entity);
                RaiseLocalEvent(heldEntity.Value, ref throwAttempt);

                if (throwAttempt.Cancelled)
                    continue;

                if (!TryDrop(entity.AsNullable(), hand, checkActionBlocker: false))
                    continue;

                // Rotate the item's throw vector a bit for each item
                var angleOffset = _random.NextAngle(-spreadMaxAngle, spreadMaxAngle);
                // Rotate the holder's velocity vector by the angle offset to get the item's velocity vector
                var itemVelocity = angleOffset.RotateVec(holderVelocity);
                // Decrease the distance of the throw by a random amount
                itemVelocity *= _random.NextFloat(1f);
                // Heavier objects don't get thrown as far
                // If the item doesn't have a physics component, it isn't going to get thrown anyway, but we'll assume infinite mass
                itemVelocity *= _physicsQuery.TryComp(heldEntity, out var heldPhysics) ? heldPhysics.InvMass : 0;
                // Throw at half the holder's intentional throw speed and
                // vary the speed a little to make it look more interesting
                var throwSpeed = entity.Comp.BaseThrowspeed * _random.NextFloat(0.45f, 0.55f);

                _throwingSystem.TryThrow(heldEntity.Value,
                    itemVelocity,
                    throwSpeed,
                    entity,
                    pushbackRatio: 0,
                    compensateFriction: false
                );
            }
        }

        #endregion
    }
}
