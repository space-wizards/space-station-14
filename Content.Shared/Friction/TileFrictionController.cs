using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Friction
{
    public sealed class TileFrictionController : VirtualController
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

        private EntityQuery<TileFrictionModifierComponent> _frictionQuery;
        private EntityQuery<TransformComponent> _xformQuery;
        private EntityQuery<PullerComponent> _pullerQuery;
        private EntityQuery<PullableComponent> _pullableQuery;
        private EntityQuery<MapGridComponent> _gridQuery;

        private float _frictionModifier;
        private float _minDamping;
        private float _airDamping;
        private float _offGridDamping;

        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configManager, CCVars.TileFrictionModifier, value => _frictionModifier = value, true);
            Subs.CVar(_configManager, CCVars.MinFriction, value => _minDamping = value, true);
            Subs.CVar(_configManager, CCVars.AirFriction, value => _airDamping = value, true);
            Subs.CVar(_configManager, CCVars.OffgridFriction, value => _offGridDamping = value, true);
            _frictionQuery = GetEntityQuery<TileFrictionModifierComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
            _pullerQuery = GetEntityQuery<PullerComponent>();
            _pullableQuery = GetEntityQuery<PullableComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
        }

        public override void UpdateBeforeMapSolve(bool prediction, PhysicsMapComponent mapComponent, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, mapComponent, frameTime);

            foreach (var body in mapComponent.AwakeBodies)
            {
                var uid = body.Owner;

                // Only apply friction when it's not a mob (or the mob doesn't have control)
                // We may want to instead only apply friction to dynamic entities and not mobs ever.
                if (prediction && !body.Predict || _mover.UseMobMovement(uid))
                    continue;

                if (body.LinearVelocity.Equals(Vector2.Zero) && body.AngularVelocity.Equals(0f))
                    continue;

                if (!_xformQuery.TryGetComponent(uid, out var xform))
                {
                    Log.Error($"Unable to get transform for {ToPrettyString(uid)} in tilefrictioncontroller");
                    continue;
                }

                float friction;

                // If we're not touching the ground, don't use tileFriction.
                // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
                if (body.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(uid, body, xform) || !xform.Coordinates.IsValid(EntityManager))
                    friction = xform.GridUid == null || !_gridQuery.HasComp(xform.GridUid) ? _offGridDamping : _airDamping;
                else
                    friction = _frictionModifier * GetTileFriction(uid, body, xform);

                var bodyModifier = 1f;

                if (_frictionQuery.TryGetComponent(uid, out var frictionComp))
                {
                    bodyModifier = frictionComp.Modifier;
                }

                var ev = new TileFrictionEvent(bodyModifier);

                RaiseLocalEvent(uid, ref ev);
                bodyModifier = ev.Modifier;

                // If we're sandwiched between 2 pullers reduce friction
                // Might be better to make this dynamic and check how many are in the pull chain?
                // Either way should be much faster for now.
                if (_pullerQuery.TryGetComponent(uid, out var puller) && puller.Pulling != null &&
                    _pullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
                {
                    bodyModifier *= 0.2f;
                }

                friction *= bodyModifier;

                friction = Math.Max(_minDamping, friction);

                PhysicsSystem.SetLinearDamping(uid, body, friction);
                PhysicsSystem.SetAngularDamping(uid, body, friction);

                if (body.BodyType != BodyType.KinematicController)
                    return;

                // Physics engine doesn't apply damping to Kinematic Controllers so we have to do it here.
                // BEWARE YE TRAVELLER:
                // You may think you can just pass the body.LinearVelocity to the Friction function and edit it there!
                // But doing so is unpredicted! And you will doom yourself to 1000 years of rubber banding!
                var velocity = body.LinearVelocity;
                _mover.Friction(0f, frameTime, friction, ref velocity);
                PhysicsSystem.SetLinearVelocity(uid, velocity, body: body);
            }
        }

        [Pure]
        private float GetTileFriction(
            EntityUid uid,
            PhysicsComponent body,
            TransformComponent xform)
        {
            var tileModifier = 1f;
            // If not on a grid and not in the air then return the map's friction.
            if (!_gridQuery.TryGetComponent(xform.GridUid, out var grid))
            {
                return _frictionQuery.TryGetComponent(xform.MapUid, out var friction)
                    ? friction.Modifier
                    : tileModifier;
            }

            var tile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);

            // If it's a map but on an empty tile then just assume it has gravity.
            if (tile.Tile.IsEmpty &&
                HasComp<MapComponent>(xform.GridUid) &&
                (!TryComp<GravityComponent>(xform.GridUid, out var gravity) || gravity.Enabled))
                return tileModifier;

            // Check for anchored ents that modify friction
            var anc = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, grid, tile.GridIndices);
            while (anc.MoveNext(out var tileEnt))
            {
                if (_frictionQuery.TryGetComponent(tileEnt, out var friction))
                    tileModifier *= friction.Modifier;
            }

            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction * tileModifier;
        }

        public void SetModifier(EntityUid entityUid, float value, TileFrictionModifierComponent? friction = null)
        {
            if (!Resolve(entityUid, ref friction) || value.Equals(friction.Modifier))
                return;

            friction.Modifier = value;
            Dirty(entityUid, friction);
        }
    }
}
