using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly ThrusterSystem _thruster = default!;
        /// <summary>
        /// These mobs will get skipped over when checking which mobs
        /// should be moved. Prediction is handled elsewhere by
        /// cancelling the movement attempt in the shared and
        /// client namespace.
        /// </summary>
        private HashSet<EntityUid> _excludedMobs = new();
        private Dictionary<ShuttleComponent, List<(PilotComponent, IMoverComponent)>> _shuttlePilots = new();

        protected override Filter GetSoundPlayers(EntityUid mover)
        {
            return Filter.Pvs(mover, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == mover);
        }

        protected override bool CanSound()
        {
            return true;
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            _excludedMobs.Clear();

            foreach (var (mobMover, mover, physics, xform) in EntityManager.EntityQuery<IMobMoverComponent, IMoverComponent, PhysicsComponent, TransformComponent>())
            {
                _excludedMobs.Add(mover.Owner);
                HandleMobMovement(mover, physics, mobMover, xform);
            }

            HandleShuttleMovement(frameTime);
            HandleVehicleMovement();

            foreach (var (mover, physics) in EntityManager.EntityQuery<IMoverComponent, PhysicsComponent>(true))
            {
                if (_excludedMobs.Contains(mover.Owner)) continue;

                HandleKinematicMovement(mover, physics);
            }
        }

        private void HandleShuttleMovement(float frameTime)
        {
            var newPilots = new Dictionary<ShuttleComponent, List<(PilotComponent, IMoverComponent)>>();

            // We just mark off their movement and the shuttle itself does its own movement
            foreach (var (pilot, mover, xform) in EntityManager.EntityQuery<PilotComponent, SharedPlayerInputMoverComponent, TransformComponent>())
            {
                if (pilot.Console == null) continue;
                _excludedMobs.Add(mover.Owner);

                var gridId = xform.GridEntityId;
                // This tries to see if the grid is a shuttle
                if (!_mapManager.TryGetGrid(gridId, out var grid) ||
                    !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) continue;

                if (!newPilots.TryGetValue(shuttleComponent, out var pilots))
                {
                    pilots = new List<(PilotComponent, IMoverComponent)>();
                    newPilots[shuttleComponent] = pilots;
                }

                pilots.Add((pilot, mover));
            }

            // Reset inputs for non-piloted shuttles.
            foreach (var (shuttle, _) in _shuttlePilots)
            {
                if (newPilots.ContainsKey(shuttle)) continue;

                _thruster.DisableLinearThrusters(shuttle);
            }

            _shuttlePilots = newPilots;

            // Collate all of the linear / angular velocites for a shuttle
            // then do the movement input once for it.
            foreach (var (shuttle, pilots) in _shuttlePilots)
            {
                if (Paused(shuttle.Owner) || !TryComp(shuttle.Owner, out PhysicsComponent? body)) continue;

                // Collate movement linear and angular inputs together
                var linearInput = Vector2.Zero;
                var angularInput = 0f;

                switch (shuttle.Mode)
                {
                    case ShuttleMode.Cruise:
                        foreach (var (pilot, mover) in pilots)
                        {
                            var console = pilot.Console;

                            if (console == null)
                            {
                                DebugTools.Assert(false);
                                continue;
                            }

                            var sprint = mover.VelocityDir.sprinting;

                            if (sprint.Equals(Vector2.Zero)) continue;

                            var offsetRotation = EntityManager.GetComponent<TransformComponent>(console.Owner).LocalRotation;

                            linearInput += offsetRotation.RotateVec(new Vector2(0f, sprint.Y));
                            angularInput += sprint.X;
                        }
                        break;
                    case ShuttleMode.Strafing:
                        // No angular input possible
                        foreach (var (pilot, mover) in pilots)
                        {
                            var console = pilot.Console;

                            if (console == null)
                            {
                                DebugTools.Assert(false);
                                continue;
                            }

                            var sprint = mover.VelocityDir.sprinting;

                            if (sprint.Equals(Vector2.Zero)) continue;

                            var offsetRotation = EntityManager.GetComponent<TransformComponent>((console).Owner).LocalRotation;
                            sprint = offsetRotation.RotateVec(sprint);

                            linearInput += sprint;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var count = pilots.Count;
                linearInput /= count;
                angularInput /= count;

                // Handle shuttle movement
                if (linearInput.Length.Equals(0f))
                {
                    _thruster.DisableLinearThrusters(shuttle);
                    body.LinearDamping = _shuttle.ShuttleIdleLinearDamping * body.InvMass;
                    if (body.LinearVelocity.Length < 0.08)
                    {
                        body.LinearVelocity = Vector2.Zero;
                    }
                }
                else
                {
                    body.LinearDamping = 0;
                    var angle = linearInput.ToWorldAngle();
                    var linearDir = angle.GetDir();
                    var dockFlag = linearDir.AsFlag();
                    var shuttleNorth = EntityManager.GetComponent<TransformComponent>(body.Owner).WorldRotation.ToWorldVec();

                    var totalForce = new Vector2();

                    // Won't just do cardinal directions.
                    foreach (DirectionFlag dir in Enum.GetValues(typeof(DirectionFlag)))
                    {
                        // Brain no worky but I just want cardinals
                        switch (dir)
                        {
                            case DirectionFlag.South:
                            case DirectionFlag.East:
                            case DirectionFlag.North:
                            case DirectionFlag.West:
                                break;
                            default:
                                continue;
                        }

                        if ((dir & dockFlag) == 0x0)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, dir);
                            continue;
                        }

                        float length;
                        Angle thrustAngle;

                        switch (dir)
                        {
                            case DirectionFlag.North:
                                length = linearInput.Y;
                                thrustAngle = new Angle(MathF.PI);
                                break;
                            case DirectionFlag.South:
                                length = -linearInput.Y;
                                thrustAngle = new Angle(0f);
                                break;
                            case DirectionFlag.East:
                                length = linearInput.X;
                                thrustAngle = new Angle(MathF.PI / 2f);
                                break;
                            case DirectionFlag.West:
                                length = -linearInput.X;
                                thrustAngle = new Angle(-MathF.PI / 2f);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _thruster.EnableLinearThrustDirection(shuttle, dir);

                        var index = (int) Math.Log2((int) dir);
                        var force = thrustAngle.RotateVec(shuttleNorth) * shuttle.LinearThrust[index] * length;

                        totalForce += force;
                    }

                    var dragForce = body.LinearVelocity * (totalForce.Length / _shuttle.ShuttleMaxLinearSpeed);
                    body.ApplyLinearImpulse((totalForce - dragForce) * frameTime);
                }

                if (MathHelper.CloseTo(angularInput, 0f))
                {
                    _thruster.SetAngularThrust(shuttle, false);
                    body.AngularDamping = _shuttle.ShuttleIdleAngularDamping * body.InvI;
                    body.SleepingAllowed = true;

                    if (Math.Abs(body.AngularVelocity) < 0.01f)
                    {
                        body.AngularVelocity = 0f;
                    }
                }
                else
                {
                    body.AngularDamping = 0;
                    body.SleepingAllowed = false;

                    var maxSpeed = Math.Min(_shuttle.ShuttleMaxAngularMomentum * body.InvI, _shuttle.ShuttleMaxAngularSpeed);
                    var maxTorque = body.Inertia * _shuttle.ShuttleMaxAngularAcc;

                    var torque = Math.Min(shuttle.AngularThrust, maxTorque);
                    var dragTorque = body.AngularVelocity * (torque / maxSpeed);

                    body.ApplyAngularImpulse((-angularInput * torque - dragTorque) * frameTime);

                    _thruster.SetAngularThrust(shuttle, true);
                }
            }
        }
        /// <summary>
        /// Add mobs riding vehicles to the list of mobs whose input
        /// should be ignored.
        /// </summary>
        private void HandleVehicleMovement()
        {
            // TODO: Nuke this code. It's on my list.
            foreach (var (rider, mover) in EntityQuery<RiderComponent, SharedPlayerInputMoverComponent>())
            {
                if (rider.Vehicle == null) continue;
                _excludedMobs.Add(mover.Owner);

                if (!_excludedMobs.Add(rider.Vehicle.Owner)) continue;

                if (!TryComp<IMoverComponent>(rider.Vehicle.Owner, out var vehicleMover) ||
                    !TryComp<PhysicsComponent>(rider.Vehicle.Owner, out var vehicleBody) ||
                    rider.Vehicle.Owner.IsWeightless(vehicleBody, mapManager: _mapManager, entityManager: EntityManager)) continue;

                HandleKinematicMovement(vehicleMover, vehicleBody);
            }
        }
    }
}
