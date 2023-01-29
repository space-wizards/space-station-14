using Content.Server.Cargo.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ThrusterSystem _thruster = default!;

        private Dictionary<ShuttleComponent, List<(PilotComponent, InputMoverComponent, TransformComponent)>> _shuttlePilots = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerAttachedEvent>(OnRelayPlayerAttached);
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerDetachedEvent>(OnRelayPlayerDetached);
            SubscribeLocalEvent<InputMoverComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InputMoverComponent, PlayerDetachedEvent>(OnPlayerDetached);
        }

        private void OnRelayPlayerAttached(EntityUid uid, RelayInputMoverComponent component, PlayerAttachedEvent args)
        {
            if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnRelayPlayerDetached(EntityUid uid, RelayInputMoverComponent component, PlayerDetachedEvent args)
        {
            if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnPlayerAttached(EntityUid uid, InputMoverComponent component, PlayerAttachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        private void OnPlayerDetached(EntityUid uid, InputMoverComponent component, PlayerDetachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        protected override bool CanSound()
        {
            return true;
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var relayQuery = GetEntityQuery<RelayInputMoverComponent>();
            var relayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var moverQuery = GetEntityQuery<InputMoverComponent>();

            foreach (var mover in EntityQuery<InputMoverComponent>(true))
            {
                var uid = mover.Owner;
                if (relayQuery.HasComponent(uid))
                    continue;

                if (!xformQuery.TryGetComponent(uid, out var xform))
                {
                    continue;
                }

                PhysicsComponent? body;
                var xformMover = xform;

                if (mover.ToParent && relayQuery.HasComponent(xform.ParentUid))
                {
                    if (!bodyQuery.TryGetComponent(xform.ParentUid, out body) ||
                        !TryComp(xform.ParentUid, out xformMover))
                    {
                        continue;
                    }
                }
                else if (!bodyQuery.TryGetComponent(uid, out body))
                {
                    continue;
                }

                HandleMobMovement(uid, mover, body, xformMover, frameTime, xformQuery, moverQuery, relayTargetQuery);
            }

            HandleShuttleMovement(frameTime);
        }

        public (Vector2 Strafe, float Rotation, float Brakes) GetPilotVelocityInput(PilotComponent component)
        {
            if (!Timing.InSimulation)
            {
                // Outside of simulation we'll be running client predicted movement per-frame.
                // So return a full-length vector as if it's a full tick.
                // Physics system will have the correct time step anyways.
                ResetSubtick(component);
                ApplyTick(component, 1f);
                return (component.CurTickStrafeMovement, component.CurTickRotationMovement, component.CurTickBraking);
            }

            float remainingFraction;

            if (Timing.CurTick > component.LastInputTick)
            {
                component.CurTickStrafeMovement = Vector2.Zero;
                component.CurTickRotationMovement = 0f;
                component.CurTickBraking = 0f;
                remainingFraction = 1;
            }
            else
            {
                remainingFraction = (ushort.MaxValue - component.LastInputSubTick) / (float) ushort.MaxValue;
            }

            ApplyTick(component, remainingFraction);

            // Logger.Info($"{curDir}{walk}{sprint}");
            return (component.CurTickStrafeMovement, component.CurTickRotationMovement, component.CurTickBraking);
        }

        private void ResetSubtick(PilotComponent component)
        {
            if (Timing.CurTick <= component.LastInputTick) return;

            component.CurTickStrafeMovement = Vector2.Zero;
            component.CurTickRotationMovement = 0f;
            component.CurTickBraking = 0f;
            component.LastInputTick = Timing.CurTick;
            component.LastInputSubTick = 0;
        }

        protected override void HandleShuttleInput(EntityUid uid, ShuttleButtons button, ushort subTick, bool state)
        {
            if (!TryComp<PilotComponent>(uid, out var pilot) || pilot.Console == null) return;

            ResetSubtick(pilot);

            if (subTick >= pilot.LastInputSubTick)
            {
                var fraction = (subTick - pilot.LastInputSubTick) / (float) ushort.MaxValue;

                ApplyTick(pilot, fraction);
                pilot.LastInputSubTick = subTick;
            }

            var buttons = pilot.HeldButtons;

            if (state)
            {
                buttons |= button;
            }
            else
            {
                buttons &= ~button;
            }

            pilot.HeldButtons = buttons;
        }

        private void ApplyTick(PilotComponent component, float fraction)
        {
            var x = 0;
            var y = 0;
            var rot = 0;
            int brake;

            if ((component.HeldButtons & ShuttleButtons.StrafeLeft) != 0x0)
            {
                x -= 1;
            }

            if ((component.HeldButtons & ShuttleButtons.StrafeRight) != 0x0)
            {
                x += 1;
            }

            component.CurTickStrafeMovement.X += x * fraction;

            if ((component.HeldButtons & ShuttleButtons.StrafeUp) != 0x0)
            {
                y += 1;
            }

            if ((component.HeldButtons & ShuttleButtons.StrafeDown) != 0x0)
            {
                y -= 1;
            }

            component.CurTickStrafeMovement.Y += y * fraction;

            if ((component.HeldButtons & ShuttleButtons.RotateLeft) != 0x0)
            {
                rot -= 1;
            }

            if ((component.HeldButtons & ShuttleButtons.RotateRight) != 0x0)
            {
                rot += 1;
            }

            component.CurTickRotationMovement += rot * fraction;

            if ((component.HeldButtons & ShuttleButtons.Brake) != 0x0)
            {
                brake = 1;
            }
            else
            {
                brake = 0;
            }

            component.CurTickBraking += brake * fraction;
        }

        private void HandleShuttleMovement(float frameTime)
        {
            var newPilots = new Dictionary<ShuttleComponent, List<(PilotComponent Pilot, InputMoverComponent Mover, TransformComponent ConsoleXform)>>();

            // We just mark off their movement and the shuttle itself does its own movement
            foreach (var (pilot, mover) in EntityManager.EntityQuery<PilotComponent, InputMoverComponent>())
            {
                var consoleEnt = pilot.Console?.Owner;

                // TODO: This is terrible. Just make a new mover and also make it remote piloting + device networks
                if (TryComp<CargoPilotConsoleComponent>(consoleEnt, out var cargoConsole))
                {
                    consoleEnt = cargoConsole.Entity;
                }

                if (!TryComp<TransformComponent>(consoleEnt, out var xform)) continue;

                var gridId = xform.GridUid;
                // This tries to see if the grid is a shuttle and if the console should work.
                if (!_mapManager.TryGetGrid(gridId, out var grid) ||
                    !EntityManager.TryGetComponent(grid.Owner, out ShuttleComponent? shuttleComponent) ||
                    !shuttleComponent.Enabled) continue;

                if (!newPilots.TryGetValue(shuttleComponent, out var pilots))
                {
                    pilots = new List<(PilotComponent, InputMoverComponent, TransformComponent)>();
                    newPilots[shuttleComponent] = pilots;
                }

                pilots.Add((pilot, mover, xform));
            }

            // Reset inputs for non-piloted shuttles.
            foreach (var (shuttle, _) in _shuttlePilots)
            {
                if (newPilots.ContainsKey(shuttle) || FTLLocked(shuttle)) continue;

                _thruster.DisableLinearThrusters(shuttle);
            }

            _shuttlePilots = newPilots;

            // Collate all of the linear / angular velocites for a shuttle
            // then do the movement input once for it.
            foreach (var (shuttle, pilots) in _shuttlePilots)
            {
                if (Paused(shuttle.Owner) || FTLLocked(shuttle) || !TryComp(shuttle.Owner, out PhysicsComponent? body)) continue;

                var shuttleNorthAngle = Transform(body.Owner).WorldRotation;

                // Collate movement linear and angular inputs together
                var linearInput = Vector2.Zero;
                var brakeInput = 0f;
                var angularInput = 0f;

                foreach (var (pilot, _, consoleXform) in pilots)
                {
                    var pilotInput = GetPilotVelocityInput(pilot);

                    if (pilotInput.Brakes > 0f)
                    {
                        brakeInput += pilotInput.Brakes;
                    }

                    if (pilotInput.Strafe.Length > 0f)
                    {
                        var offsetRotation = consoleXform.LocalRotation;
                        linearInput += offsetRotation.RotateVec(pilotInput.Strafe);
                    }

                    if (pilotInput.Rotation != 0f)
                    {
                        angularInput += pilotInput.Rotation;
                    }
                }

                var count = pilots.Count;
                linearInput /= count;
                angularInput /= count;
                brakeInput /= count;

                /*
                 * So essentially:
                 * 1. We do the same calcs for braking as we do for linear thrust so it's similar to a player pressing it
                 * but we also need to handle when they get close to 0 hence why it sets velocity directly.
                 *
                 * 2. We do a similar calculation to mob movement where the closer you are to your speed cap the slower you accelerate
                 *
                 * TODO: Could combine braking linear input and thrust more but my brain was just not working debugging
                 * TODO: Need to have variable speed caps based on thruster count or whatever
                 */

                // Handle shuttle movement
                if (brakeInput > 0f)
                {
                    if (body.LinearVelocity.Length > 0f)
                    {
                        // Get velocity relative to the shuttle so we know which thrusters to fire
                        var shuttleVelocity = (-shuttleNorthAngle).RotateVec(body.LinearVelocity);
                        var force = Vector2.Zero;

                        if (shuttleVelocity.X < 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.West);
                            _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.East);

                            var index = (int) Math.Log2((int) DirectionFlag.East);
                            force.X += shuttle.LinearThrust[index];
                        }
                        else if (shuttleVelocity.X > 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.East);
                            _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.West);

                            var index = (int) Math.Log2((int) DirectionFlag.West);
                            force.X -= shuttle.LinearThrust[index];
                        }

                        if (shuttleVelocity.Y < 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.South);
                            _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.North);

                            var index = (int) Math.Log2((int) DirectionFlag.North);
                            force.Y += shuttle.LinearThrust[index];
                        }
                        else if (shuttleVelocity.Y > 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.North);
                            _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.South);

                            var index = (int) Math.Log2((int) DirectionFlag.South);
                            force.Y -= shuttle.LinearThrust[index];
                        }

                        var impulse = force * brakeInput;
                        var wishDir = impulse.Normalized;
                        // TODO: Adjust max possible speed based on total thrust in particular direction.
                        var wishSpeed = 20f;

                        var currentSpeed = Vector2.Dot(shuttleVelocity, wishDir);
                        var addSpeed = wishSpeed - currentSpeed;

                        if (addSpeed > 0f)
                        {
                            var accelSpeed = impulse.Length * frameTime;
                            accelSpeed = MathF.Min(accelSpeed, addSpeed);
                            impulse = impulse.Normalized * accelSpeed * body.InvMass;

                            // Cap inputs
                            if (shuttleVelocity.X < 0f)
                            {
                                impulse.X = MathF.Min(impulse.X, -shuttleVelocity.X);
                            }
                            else if (shuttleVelocity.X > 0f)
                            {
                                impulse.X = MathF.Max(impulse.X, -shuttleVelocity.X);
                            }

                            if (shuttleVelocity.Y < 0f)
                            {
                                impulse.Y = MathF.Min(impulse.Y, -shuttleVelocity.Y);
                            }
                            else if (shuttleVelocity.Y > 0f)
                            {
                                impulse.Y = MathF.Max(impulse.Y, -shuttleVelocity.Y);
                            }

                            PhysicsSystem.SetLinearVelocity(shuttle.Owner, body.LinearVelocity + shuttleNorthAngle.RotateVec(impulse), body: body);
                        }
                    }
                    else
                    {
                        _thruster.DisableLinearThrusters(shuttle);
                    }

                    if (body.AngularVelocity != 0f)
                    {
                        var impulse = shuttle.AngularThrust * brakeInput * (body.AngularVelocity > 0f ? -1f : 1f);
                        var wishSpeed = MathF.PI;

                        if (impulse < 0f)
                            wishSpeed *= -1f;

                        var currentSpeed = body.AngularVelocity;
                        var addSpeed = wishSpeed - currentSpeed;

                        if (!addSpeed.Equals(0f))
                        {
                            var accelSpeed = impulse * body.InvI * frameTime;

                            if (accelSpeed < 0f)
                                accelSpeed = MathF.Max(accelSpeed, addSpeed);
                            else
                                accelSpeed = MathF.Min(accelSpeed, addSpeed);

                            if (body.AngularVelocity < 0f && body.AngularVelocity + accelSpeed > 0f)
                                accelSpeed = -body.AngularVelocity;
                            else if (body.AngularVelocity > 0f && body.AngularVelocity + accelSpeed < 0f)
                                accelSpeed = -body.AngularVelocity;

                            PhysicsSystem.SetAngularVelocity(shuttle.Owner, body.AngularVelocity + accelSpeed, body: body);
                            _thruster.SetAngularThrust(shuttle, true);
                        }
                    }
                }

                if (linearInput.Length.Equals(0f))
                {
                    PhysicsSystem.SetSleepingAllowed(shuttle.Owner, body, true);

                    if (brakeInput.Equals(0f))
                        _thruster.DisableLinearThrusters(shuttle);

                    if (body.LinearVelocity.Length < 0.08)
                    {
                        PhysicsSystem.SetLinearVelocity(shuttle.Owner, Vector2.Zero, body: body);
                    }
                }
                else
                {
                    PhysicsSystem.SetSleepingAllowed(shuttle.Owner, body, false);
                    var angle = linearInput.ToWorldAngle();
                    var linearDir = angle.GetDir();
                    var dockFlag = linearDir.AsFlag();

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

                        var index = (int) Math.Log2((int) dir);
                        var thrust = shuttle.LinearThrust[index];

                        switch (dir)
                        {
                            case DirectionFlag.North:
                                totalForce.Y += thrust;
                                break;
                            case DirectionFlag.South:
                                totalForce.Y -= thrust;
                                break;
                            case DirectionFlag.East:
                                totalForce.X += thrust;
                                break;
                            case DirectionFlag.West:
                                totalForce.X -= thrust;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _thruster.EnableLinearThrustDirection(shuttle, dir);
                    }

                    // We don't want to touch damping if no inputs are given
                    // so we'll just add an artifical drag to the velocity input.
                    var shuttleVelocity = (-shuttleNorthAngle).RotateVec(body.LinearVelocity);

                    var wishDir = totalForce.Normalized;
                    // TODO: Adjust max possible speed based on total thrust in particular direction.
                    var wishSpeed = 20f;

                    var currentSpeed = Vector2.Dot(shuttleVelocity, wishDir);
                    var addSpeed = wishSpeed - currentSpeed;

                    if (addSpeed > 0f)
                    {
                        var accelSpeed = totalForce.Length * frameTime;
                        accelSpeed = MathF.Min(accelSpeed, addSpeed);
                        PhysicsSystem.ApplyLinearImpulse(shuttle.Owner, shuttleNorthAngle.RotateVec(totalForce.Normalized * accelSpeed), body: body);
                    }
                }

                if (MathHelper.CloseTo(angularInput, 0f))
                {
                    _thruster.SetAngularThrust(shuttle, false);
                    PhysicsSystem.SetSleepingAllowed(shuttle.Owner, body, true);

                    if (Math.Abs(body.AngularVelocity) < 0.01f)
                    {
                        PhysicsSystem.SetAngularVelocity(shuttle.Owner, 0f, body: body);
                    }
                }
                else
                {
                    PhysicsSystem.SetSleepingAllowed(shuttle.Owner, body, false);
                    var impulse = shuttle.AngularThrust * -angularInput;
                    var wishSpeed = MathF.PI;

                    if (impulse < 0f)
                        wishSpeed *= -1f;

                    var currentSpeed = body.AngularVelocity;
                    var addSpeed = wishSpeed - currentSpeed;

                    if (!addSpeed.Equals(0f))
                    {
                        var accelSpeed = impulse * body.InvI * frameTime;

                        if (accelSpeed < 0f)
                            accelSpeed = MathF.Max(accelSpeed, addSpeed);
                        else
                            accelSpeed = MathF.Min(accelSpeed, addSpeed);

                        PhysicsSystem.SetAngularVelocity(shuttle.Owner, body.AngularVelocity + accelSpeed, body: body);
                        _thruster.SetAngularThrust(shuttle, true);
                    }
                }
            }
        }

        private bool FTLLocked(ShuttleComponent shuttle)
        {
            return (TryComp<FTLComponent>(shuttle.Owner, out var ftl) &&
                    (ftl.State & (FTLState.Starting | FTLState.Travelling | FTLState.Arriving)) != 0x0);
        }

    }
}
