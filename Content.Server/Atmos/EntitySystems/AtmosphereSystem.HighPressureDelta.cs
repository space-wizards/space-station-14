using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private const int SpaceWindSoundCooldownCycles = 75;

        private int _spaceWindSoundCooldown = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? SpaceWindSound { get; private set; } = "/Audio/Effects/space_wind.ogg";

        private readonly HashSet<Entity<MovedByPressureComponent>> _activePressures = new(8);

        private void UpdateHighPressure(float frameTime)
        {
            var toRemove = new RemQueue<Entity<MovedByPressureComponent>>();

            foreach (var ent in _activePressures)
            {
                var (uid, comp) = ent;
                MetaDataComponent? metadata = null;

                if (Deleted(uid, metadata))
                {
                    toRemove.Add((uid, comp));
                    continue;
                }

                if (Paused(uid, metadata))
                    continue;

                comp.Accumulator += frameTime;

                if (comp.Accumulator < 2f)
                    continue;

                // Reset it just for VV reasons even though it doesn't matter
                comp.Accumulator = 0f;
                toRemove.Add(ent);

                if (TryComp<PhysicsComponent>(uid, out var body))
                {
                    _physics.SetBodyStatus(uid, body, BodyStatus.OnGround);
                }

                if (TryComp<FixturesComponent>(uid, out var fixtures))
                {
                    foreach (var (id, fixture) in fixtures.Fixtures)
                    {
                        _physics.AddCollisionMask(uid, id, fixture, (int) CollisionGroup.TableLayer, manager: fixtures);
                    }
                }
            }

            foreach (var comp in toRemove)
            {
                _activePressures.Remove(comp);
            }
        }

        private void HighPressureMovements(Entity<GridAtmosphereComponent> gridAtmosphere, TileAtmosphere tile, EntityQuery<PhysicsComponent> bodies, EntityQuery<TransformComponent> xforms, EntityQuery<MovedByPressureComponent> pressureQuery, EntityQuery<MetaDataComponent> metas)
        {
            if (tile.PressureDifference < SpaceWindMinimumCalculatedMass * SpaceWindMinimumCalculatedMass)
                return;
            // TODO ATMOS finish this

            // Don't play the space wind sound on tiles that are on fire...
            if (tile.PressureDifference > 15 && !tile.Hotspot.Valid)
            {
                if (_spaceWindSoundCooldown == 0 && !string.IsNullOrEmpty(SpaceWindSound))
                {
                    var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);
                    _audio.PlayPvs(SpaceWindSound, coordinates, AudioParams.Default.WithVariation(0.125f).WithVolume(MathHelper.Clamp(tile.PressureDifference / 10, 10, 100)));
                }
            }


            if (tile.PressureDifference > 100)
            {
                // TODO ATMOS Do space wind graphics here!
            }

            if (_spaceWindSoundCooldown++ > SpaceWindSoundCooldownCycles)
                _spaceWindSoundCooldown = 0;

            // No atmos yeets, return early.
            if (!SpaceWind)
                return;

            // Used by ExperiencePressureDifference to correct push/throw directions from tile-relative to physics world.
            var gridWorldRotation = xforms.GetComponent(gridAtmosphere).WorldRotation;

            // If we're using monstermos, smooth out the yeet direction to follow the flow
            //TODO This is bad, don't run this. It just makes the throws worse by somehow rounding them to orthogonal
            if (!MonstermosEqualization)
            {
                // We step through tiles according to the pressure direction on the current tile.
                // The goal is to get a general direction of the airflow in the area.
                // 3 is the magic number - enough to go around corners, but not U-turns.
                var curTile = tile;
                for (var i = 0; i < 3; i++)
                {
                    if (curTile.PressureDirection == AtmosDirection.Invalid
                        || !curTile.AdjacentBits.IsFlagSet(curTile.PressureDirection))
                        break;
                    curTile = curTile.AdjacentTiles[curTile.PressureDirection.ToIndex()]!;
                }

                if (curTile != tile)
                    tile.PressureSpecificTarget = curTile;
            }

            _entSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(tile.GridIndex, tile.GridIndices, _entSet, 0f);

            foreach (var entity in _entSet)
            {
                // Ideally containers would have their own EntityQuery internally or something given recursively it may need to slam GetComp<T> anyway.
                // Also, don't care about static bodies (but also due to collisionwakestate can't query dynamic directly atm).
                if (!bodies.TryGetComponent(entity, out var body) ||
                    !pressureQuery.TryGetComponent(entity, out var pressure) ||
                    !pressure.Enabled)
                    continue;

                if (_containers.IsEntityInContainer(entity, metas.GetComponent(entity))) continue;

                var pressureMovements = EnsureComp<MovedByPressureComponent>(entity);
                if (pressure.LastHighPressureMovementAirCycle < gridAtmosphere.Comp.UpdateCounter)
                {
                    // tl;dr YEET
                    ExperiencePressureDifference(
                        (entity, pressureMovements),
                        gridAtmosphere.Comp.UpdateCounter,
                        tile.PressureDifference,
                        tile.PressureDirection,
                        tile.PressureSpecificTarget != null ? _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.PressureSpecificTarget.GridIndices) : EntityCoordinates.Invalid,
                        gridWorldRotation,
                        xforms.GetComponent(entity),
                        body);
                }
            }
        }

        // Called from AtmosphereSystem.LINDA.cs with SpaceWind CVar check handled there.
        private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, AtmosDirection differenceDirection, float difference)
        {
            gridAtmosphere.HighPressureDelta.Add(tile);

            if (difference <= tile.PressureDifference)
                return;

            tile.PressureDifference = difference;
            tile.PressureDirection = differenceDirection;
        }

        //INFO The EE version of this function drops pressureResistanceProbDelta, since it's not needed. If you are for whatever reason calling this function
        //INFO And if it isn't working, you've probably still got the pressureResistanceProbDelta line included.
        /// <notes>
        /// EXPLANATION:
        /// pressureDifference = Force of Air Flow on a given tile
        /// physics.Mass = Mass of the object potentially being thrown
        /// physics.InvMass = 1 divided by said Mass. More CPU efficient way to do division.
        ///
        /// Objects can only be thrown if the force of air flow is greater than the SQUARE of their mass or {SpaceWindMinimumCalculatedMass}, whichever is heavier
        /// This means that the heavier an object is, the exponentially more force is required to move it
        /// The force of a throw is equal to the force of air pressure, divided by an object's mass. So not only are heavier objects
        /// less likely to be thrown, they are also harder to throw,
        /// while lighter objects are yeeted easily, and from great distance.
        ///
        /// For a human sized entity with a standard weight of 80kg and a spacing between a hard vacuum and a room pressurized at 101kpa,
        /// The human shall only be moved if he is either very close to the hole, or is standing in a region of high airflow
        /// </notes>

        public void ExperiencePressureDifference(
            Entity<MovedByPressureComponent> ent,
            int cycle,
            float pressureDifference,
            AtmosDirection direction,
            EntityCoordinates throwTarget,
            Angle gridWorldRotation,
            TransformComponent? xform = null,
            PhysicsComponent? physics = null)
        {
            var (uid, component) = ent;
            if (!Resolve(uid, ref physics, false))
                return;

            if (!Resolve(uid, ref xform))
                return;

            if (physics.BodyType != BodyType.Static
                && !float.IsPositiveInfinity(component.MoveResist))
            {
                var moveForce = pressureDifference * MathF.Max(physics.InvMass, SpaceWindMaximumCalculatedInverseMass);
                if (HasComp<HumanoidAppearanceComponent>(ent))
                    moveForce *= HumanoidThrowMultiplier;
                if (moveForce > physics.Mass)
                {
                    // Grid-rotation adjusted direction
                    var dirVec = (direction.ToAngle() + gridWorldRotation).ToWorldVec();
                    moveForce *= MathF.Max(physics.InvMass, SpaceWindMaximumCalculatedInverseMass);

                    //TODO Consider replacing throw target with proper trigonometry angles.
                    if (throwTarget != EntityCoordinates.Invalid)
                    {
                        var pos = throwTarget.ToMap(EntityManager, _transformSystem).Position - xform.WorldPosition + dirVec;
                        _throwing.TryThrow(uid, pos.Normalized() * MathF.Min(moveForce, SpaceWindMaxVelocity), moveForce);
                    }
                    else
                    {
                        _throwing.TryThrow(uid, dirVec.Normalized() * MathF.Min(moveForce, SpaceWindMaxVelocity), moveForce);
                    }

                    component.LastHighPressureMovementAirCycle = cycle;
                }
            }
        }
    }
}
