using Content.Shared.CCVar;
using Content.Shared.Gravity;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;


namespace Content.Shared.Friction
{
    public sealed class TileFrictionController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private float _stopSpeed;
        private float _frictionModifier;
        private const float DefaultFriction = 0.3f;

        public override void Initialize()
        {
            base.Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.OnValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier, true);
            configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);

            SubscribeLocalEvent<TileFrictionModifierComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<TileFrictionModifierComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, TileFrictionModifierComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not TileFrictionComponentState tileState) return;
            component.Modifier = tileState.Modifier;
        }

        private void OnGetState(EntityUid uid, TileFrictionModifierComponent component, ref ComponentGetState args)
        {
            args.State = new TileFrictionComponentState(component.Modifier);
        }

        private void SetStopSpeed(float value) => _stopSpeed = value;

        private void SetFrictionModifier(float value) => _frictionModifier = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.UnsubValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier);
            configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateBeforeMapSolve(bool prediction, PhysicsMapComponent mapComponent, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, mapComponent, frameTime);

            var frictionQuery = GetEntityQuery<TileFrictionModifierComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var pullerQuery = GetEntityQuery<SharedPullerComponent>();
            var pullableQuery = GetEntityQuery<SharedPullableComponent>();

            foreach (var body in mapComponent.AwakeBodies)
            {
                // Only apply friction when it's not a mob (or the mob doesn't have control)
                if (prediction && !body.Predict ||
                    body.BodyStatus == BodyStatus.InAir ||
                    _mover.UseMobMovement(body.Owner))
                {
                    continue;
                }

                if (body.LinearVelocity.Equals(Vector2.Zero) && body.AngularVelocity.Equals(0f)) continue;

                DebugTools.Assert(!Deleted(body.Owner));

                if (!xformQuery.TryGetComponent(body.Owner, out var xform))
                {
                    Logger.ErrorS("physics", $"Unable to get transform for {ToPrettyString(body.Owner)} in tilefrictioncontroller");
                    continue;
                }

                var surfaceFriction = GetTileFriction(body, xform);
                var bodyModifier = 1f;

                if (frictionQuery.TryGetComponent(body.Owner, out var frictionComp))
                {
                    bodyModifier = frictionComp.Modifier;
                }

                var ev = new TileFrictionEvent(bodyModifier);

                RaiseLocalEvent(body.Owner, ref ev);
                bodyModifier = ev.Modifier;

                // If we're sandwiched between 2 pullers reduce friction
                // Might be better to make this dynamic and check how many are in the pull chain?
                // Either way should be much faster for now.
                if (pullerQuery.TryGetComponent(body.Owner, out var puller) && puller.Pulling != null &&
                    pullableQuery.TryGetComponent(body.Owner, out var pullable) && pullable.BeingPulled)
                {
                    bodyModifier *= 0.2f;
                }

                var friction = _frictionModifier * surfaceFriction * bodyModifier;

                ReduceLinearVelocity(prediction, body, friction, frameTime);
                ReduceAngularVelocity(prediction, body, friction, frameTime);
            }
        }

        private void ReduceLinearVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = body.LinearVelocity.Length;

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            _physics.SetLinearVelocity(body, body.LinearVelocity * newSpeed);
        }

        private void ReduceAngularVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = MathF.Abs(body.AngularVelocity);

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            _physics.SetAngularVelocity(body, body.AngularVelocity * newSpeed);
        }

        [Pure]
        private float GetTileFriction(PhysicsComponent body, TransformComponent xform)
        {
            // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
            if (_gravity.IsWeightless(body.Owner, body, xform))
                return 0.0f;

            if (!xform.Coordinates.IsValid(EntityManager)) return 0.0f;

            if (_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                var tile = grid.GetTileRef(xform.Coordinates);

                // If it's a map but on an empty tile then just assume it has gravity.
                if (tile.Tile.IsEmpty && HasComp<MapComponent>(xform.GridUid) &&
                    (!TryComp<GravityComponent>(xform.GridUid, out var gravity) || gravity.Enabled))
                {
                    return DefaultFriction;
                }

                var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
                return tileDef.Friction;
            }

            return TryComp<TileFrictionModifierComponent>(xform.MapUid, out var friction) ? friction.Modifier : DefaultFriction;
        }

        [NetSerializable, Serializable]
        private sealed class TileFrictionComponentState : ComponentState
        {
            public float Modifier;

            public TileFrictionComponentState(float modifier)
            {
                Modifier = modifier;
            }
        }
    }
}
