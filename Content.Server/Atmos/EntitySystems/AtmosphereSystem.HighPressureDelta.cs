using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private static readonly ProtoId<SoundCollectionPrototype> DefaultSpaceWindSounds = "SpaceWind";

        private const int SpaceWindSoundCooldownCycles = 75;

        private int _spaceWindSoundCooldown = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier? SpaceWindSound { get; private set; } = new SoundCollectionSpecifier(DefaultSpaceWindSounds, AudioParams.Default.WithVariation(0.125f));

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

                if (HasComp<MobStateComponent>(uid) &&
                    TryComp<PhysicsComponent>(uid, out var body))
                {
                    _physics.SetBodyStatus(uid, body, BodyStatus.OnGround);
                }

                if (TryComp<FixturesComponent>(uid, out var fixtures)
                    && TryComp<MovedByPressureComponent>(uid, out var component))
                {
                    foreach (var (id, fixture) in fixtures.Fixtures)
                    {
                        if (component.TableLayerRemoved.Contains(id))
                        {
                            _physics.AddCollisionMask(uid, id, fixture, (int)CollisionGroup.TableLayer, manager: fixtures);
                        }
                    }
                }
            }

            foreach (var comp in toRemove)
            {
                _activePressures.Remove(comp);
            }
        }

        private void AddMobMovedByPressure(EntityUid uid, MovedByPressureComponent component, PhysicsComponent body)
        {
            if (!TryComp<FixturesComponent>(uid, out var fixtures))
                return;

            _physics.SetBodyStatus(uid, body, BodyStatus.InAir);

            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                // Mark fixtures that have TableLayer removed
                if ((fixture.CollisionMask & (int)CollisionGroup.TableLayer) != 0)
                {
                    component.TableLayerRemoved.Add(id);
                    _physics.RemoveCollisionMask(uid, id, fixture, (int)CollisionGroup.TableLayer, manager: fixtures);
                }
            }
            // TODO: Make them dynamic type? Ehh but they still want movement so uhh make it non-predicted like weightless?
            // idk it's hard.

            component.Accumulator = 0f;
            _activePressures.Add((uid, component));
        }

        private void HighPressureMovements(Entity<GridAtmosphereComponent> gridAtmosphere, TileAtmosphere tile, EntityQuery<PhysicsComponent> bodies, EntityQuery<TransformComponent> xforms, EntityQuery<MovedByPressureComponent> pressureQuery, EntityQuery<MetaDataComponent> metas)
        {
            // TODO ATMOS finish this

            // Don't play the space wind sound on tiles that are on fire...
            if (tile.PressureDifference > 15 && !tile.Hotspot.Valid)
            {
                if (_spaceWindSoundCooldown == 0 && SpaceWindSound != null)
                {
                    var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);
                    _audio.PlayPvs(SpaceWindSound, coordinates, SpaceWindSound.Params.WithVolume(MathHelper.Clamp(tile.PressureDifference / 10, 10, 100)));
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
            var gridWorldRotation = _transformSystem.GetWorldRotation(gridAtmosphere);

            // If we're using monstermos, smooth out the yeet direction to follow the flow
            if (MonstermosEqualization)
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
                        tile.PressureDirection, 0,
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

        public void ExperiencePressureDifference(
            Entity<MovedByPressureComponent> ent,
            int cycle,
            float pressureDifference,
            AtmosDirection direction,
            float pressureResistanceProbDelta,
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

            // TODO ATMOS stuns?

            var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
            var moveProb = 100f;

            if (component.PressureResistance > 0)
                moveProb = MathF.Abs((pressureDifference / component.PressureResistance * MovedByPressureComponent.ProbabilityBasePercent) -
                                     MovedByPressureComponent.ProbabilityOffset);

            // Can we yeet the thing (due to probability, strength, etc.)
            if (moveProb > MovedByPressureComponent.ProbabilityOffset && _random.Prob(MathF.Min(moveProb / 100f, 1f))
                                                                      && !float.IsPositiveInfinity(component.MoveResist)
                                                                      && (physics.BodyType != BodyType.Static
                                                                          && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForcePushRatio)))
                || (physics.BodyType == BodyType.Static && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForceForcePushRatio))))
            {
                if (HasComp<MobStateComponent>(uid))
                {
                    AddMobMovedByPressure(uid, component, physics);
                }

                if (maxForce > MovedByPressureComponent.ThrowForce)
                {
                    var moveForce = maxForce;
                    moveForce /= (throwTarget != EntityCoordinates.Invalid) ? SpaceWindPressureForceDivisorThrow : SpaceWindPressureForceDivisorPush;
                    moveForce *= MathHelper.Clamp(moveProb, 0, 100);

                    // Apply a sanity clamp to prevent being thrown through objects.
                    var maxSafeForceForObject = SpaceWindMaxVelocity * physics.Mass;
                    moveForce = MathF.Min(moveForce, maxSafeForceForObject);

                    // Grid-rotation adjusted direction
                    var dirVec = (direction.ToAngle() + gridWorldRotation).ToWorldVec();

                    // TODO: Technically these directions won't be correct but uhh I'm just here for optimisations buddy not to fix my old bugs.
                    if (throwTarget != EntityCoordinates.Invalid)
                    {
                        var pos = ((_transformSystem.ToMapCoordinates(throwTarget).Position - _transformSystem.GetWorldPosition(xform)).Normalized() + dirVec).Normalized();
                        _physics.ApplyLinearImpulse(uid, pos * moveForce, body: physics);
                    }
                    else
                    {
                        moveForce = MathF.Min(moveForce, SpaceWindMaxPushForce);
                        _physics.ApplyLinearImpulse(uid, dirVec * moveForce, body: physics);
                    }

                    component.LastHighPressureMovementAirCycle = cycle;
                }
            }
        }
    }
}
