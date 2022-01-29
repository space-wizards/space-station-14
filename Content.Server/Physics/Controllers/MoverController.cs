using System;
using System.Collections.Generic;
using Content.Server.Movement.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Movement;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Physics.Controllers
{
    public class MoverController : SharedMoverController
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private float _shuttleDockSpeedCap;

        private HashSet<EntityUid> _excludedMobs = new();
        private Dictionary<ShuttleComponent, List<(PilotComponent, IMoverComponent)>> _shuttlePilots = new();

        public override void Initialize()
        {
            base.Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.ShuttleDockSpeedCap, value => _shuttleDockSpeedCap = value, true);
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            _excludedMobs.Clear();

            foreach (var (mobMover, mover, physics) in EntityManager.EntityQuery<IMobMoverComponent, IMoverComponent, PhysicsComponent>())
            {
                _excludedMobs.Add(mover.Owner);
                HandleMobMovement(mover, physics, mobMover);
            }

            HandleShuttleMovement(frameTime);

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

                var gridId = xform.GridID;

                if (!_mapManager.TryGetGrid(gridId, out var grid) ||
                    !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttleComponent)) continue;

                if (!newPilots.TryGetValue(shuttleComponent, out var pilots))
                {
                    pilots = new List<(PilotComponent, IMoverComponent)>();
                    newPilots[shuttleComponent] = pilots;
                }

                pilots.Add((pilot, mover));
            }

            var shuttleSystem = EntitySystem.Get<ShuttleSystem>();
            var thrusterSystem = EntitySystem.Get<ThrusterSystem>();

            // Reset inputs for non-piloted shuttles.
            foreach (var (shuttle, _) in _shuttlePilots)
            {
                if (newPilots.ContainsKey(shuttle)) continue;

                thrusterSystem.DisableLinearThrusters(shuttle);
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
                    case ShuttleMode.Docking:
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
                    thrusterSystem.DisableLinearThrusters(shuttle);
                    body.LinearDamping = shuttleSystem.ShuttleIdleLinearDamping;
                }
                else
                {
                    body.LinearDamping = shuttleSystem.ShuttleMovingLinearDamping;

                    var angle = linearInput.ToWorldAngle();
                    var linearDir = angle.GetDir();
                    var dockFlag = linearDir.AsFlag();
                    var shuttleNorth = EntityManager.GetComponent<TransformComponent>(body.Owner).WorldRotation.ToWorldVec();

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
                            thrusterSystem.DisableLinearThrustDirection(shuttle, dir);
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

                        thrusterSystem.EnableLinearThrustDirection(shuttle, dir);

                        var index = (int) Math.Log2((int) dir);
                        var speed = shuttle.LinearThrusterImpulse[index] * length;

                        if (body.LinearVelocity.LengthSquared < 0.5f)
                        {
                            speed *= 5f;
                        }

                        body.ApplyLinearImpulse(
                            thrustAngle.RotateVec(shuttleNorth) *
                            speed *
                            frameTime);
                    }
                }

                if (MathHelper.CloseTo(angularInput, 0f))
                {
                    thrusterSystem.SetAngularThrust(shuttle, false);
                    body.AngularDamping = shuttleSystem.ShuttleIdleAngularDamping;
                }
                else
                {
                    body.AngularDamping = shuttleSystem.ShuttleMovingAngularDamping;
                    var angularSpeed = shuttle.AngularThrust;

                    if (body.AngularVelocity < 0.5f)
                    {
                        angularSpeed *= 5f;
                    }

                    // Scale rotation by mass just to make rotating larger things a bit more bearable.
                    body.ApplyAngularImpulse(
                        -angularInput *
                        angularSpeed *
                        frameTime *
                        body.Mass / 100f);

                    thrusterSystem.SetAngularThrust(shuttle, true);
                }
            }
        }

        protected override void HandleFootsteps(IMoverComponent mover, IMobMoverComponent mobMover)
        {
            if (!mover.Owner.HasTag("FootstepSound")) return;

            var transform = EntityManager.GetComponent<TransformComponent>(mover.Owner);
            var coordinates = transform.Coordinates;
            var gridId = coordinates.GetGridId(EntityManager);
            var distanceNeeded = mover.Sprinting ? StepSoundMoveDistanceRunning : StepSoundMoveDistanceWalking;

            // Handle footsteps.
            if (_mapManager.GridExists(gridId))
            {
                // Can happen when teleporting between grids.
                if (!coordinates.TryDistance(EntityManager, mobMover.LastPosition, out var distance) ||
                    distance > distanceNeeded)
                {
                    mobMover.StepSoundDistance = distanceNeeded;
                }
                else
                {
                    mobMover.StepSoundDistance += distance;
                }
            }
            else
            {
                // In space no one can hear you squeak
                return;
            }

            DebugTools.Assert(gridId != GridId.Invalid);
            mobMover.LastPosition = coordinates;

            if (mobMover.StepSoundDistance < distanceNeeded) return;

            mobMover.StepSoundDistance -= distanceNeeded;

            var invSystem = EntitySystem.Get<InventorySystem>();

            if (invSystem.TryGetSlotEntity(mover.Owner, "shoes", out var shoes) &&
                EntityManager.TryGetComponent<FootstepModifierComponent>(shoes, out var modifier))
            {
                modifier.PlayFootstep();
            }
            else
            {
                PlayFootstepSound(mover.Owner, gridId, coordinates, mover.Sprinting);
            }
        }

        private void PlayFootstepSound(EntityUid mover, GridId gridId, EntityCoordinates coordinates, bool sprinting)
        {
            var grid = _mapManager.GetGrid(gridId);
            var tile = grid.GetTileRef(coordinates);

            if (tile.IsSpace(_tileDefinitionManager)) return;

            // If the coordinates have a FootstepModifier component
            // i.e. component that emit sound on footsteps emit that sound
            string? soundToPlay = null;
            foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
            {
                if (EntityManager.TryGetComponent(maybeFootstep, out FootstepModifierComponent? footstep))
                {
                    soundToPlay = footstep.SoundCollection.GetSound();
                    break;
                }
            }
            // if there is no FootstepModifierComponent, determine sound based on tiles
            if (soundToPlay == null)
            {
                // Walking on a tile.
                var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
                soundToPlay = def.FootstepSounds?.GetSound();
                if (string.IsNullOrEmpty(soundToPlay))
                    return;
            }

            if (string.IsNullOrWhiteSpace(soundToPlay))
            {
                Logger.ErrorS("sound", $"Unable to find sound in {nameof(PlayFootstepSound)}");
                return;
            }

            SoundSystem.Play(
                Filter.Pvs(coordinates),
                soundToPlay,
                EntityManager.GetComponent<TransformComponent>(mover).Coordinates,
                sprinting ? AudioParams.Default.WithVolume(0.75f) : null);
        }
    }
}
