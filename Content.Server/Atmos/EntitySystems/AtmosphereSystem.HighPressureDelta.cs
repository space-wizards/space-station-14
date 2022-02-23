using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.MobState.Components;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private const int SpaceWindSoundCooldownCycles = 75;

        private int _spaceWindSoundCooldown = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        public string? SpaceWindSound { get; private set; } = "/Audio/Effects/space_wind.ogg";

        private HashSet<MovedByPressureComponent> _activePressures = new(8);

        private void UpdateHighPressure(float frameTime)
        {
            var toRemove = new RemQueue<MovedByPressureComponent>();

            foreach (var comp in _activePressures)
            {
                MetaDataComponent? metadata = null;

                if (Deleted(comp.Owner, metadata))
                {
                    toRemove.Add(comp);
                    continue;
                }

                if (Paused(comp.Owner, metadata)) continue;

                comp.Accumulator += frameTime;

                if (comp.Accumulator < 2f) continue;

                // Reset it just for VV reasons even though it doesn't matter
                comp.Accumulator = 0f;
                toRemove.Add(comp);

                if (HasComp<MobStateComponent>(comp.Owner) &&
                    TryComp<PhysicsComponent>(comp.Owner, out var body))
                {
                    body.BodyStatus = BodyStatus.OnGround;
                }

                if (TryComp<FixturesComponent>(comp.Owner, out var fixtures))
                {
                    foreach (var (_, fixture) in fixtures.Fixtures)
                    {
                        _physics.AddCollisionMask(fixtures, fixture, (int) CollisionGroup.VaultImpassable);
                    }
                }
            }

            foreach (var comp in toRemove)
            {
                _activePressures.Remove(comp);
            }
        }

        private void AddMobMovedByPressure(MovedByPressureComponent component, PhysicsComponent body)
        {
            if (!TryComp<FixturesComponent>(component.Owner, out var fixtures)) return;

            body.BodyStatus = BodyStatus.InAir;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                _physics.RemoveCollisionMask(fixtures, fixture, (int) CollisionGroup.VaultImpassable);
            }

            // TODO: Make them dynamic type? Ehh but they still want movement so uhh make it non-predicted like weightless?
            // idk it's hard.

            component.Accumulator = 0f;
            _activePressures.Add(component);
        }

        private void HighPressureMovements(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, EntityQuery<PhysicsComponent> bodies, EntityQuery<TransformComponent> xforms, EntityQuery<MovedByPressureComponent> pressureQuery)
        {
            // TODO ATMOS finish this

            // Don't play the space wind sound on tiles that are on fire...
            if(tile.PressureDifference > 15 && !tile.Hotspot.Valid)
            {
                if(_spaceWindSoundCooldown == 0 && !string.IsNullOrEmpty(SpaceWindSound))
                {
                    var coordinates = tile.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager);
                    SoundSystem.Play(Filter.Pvs(coordinates), SpaceWindSound, coordinates,
                        AudioHelpers.WithVariation(0.125f).WithVolume(MathHelper.Clamp(tile.PressureDifference / 10, 10, 100)));
                }
            }

            foreach (var entity in _lookup.GetEntitiesIntersecting(tile.GridIndex, tile.GridIndices))
            {
                // Ideally containers would have their own EntityQuery internally or something given recursively it may need to slam GetComp<T> anyway.
                // Also, don't care about static bodies (but also due to collisionwakestate can't query dynamic directly atm).
                if (!bodies.TryGetComponent(entity, out var body) ||
                    !pressureQuery.TryGetComponent(entity, out var pressure) ||
                    !pressure.Enabled)
                    continue;

                var xform = xforms.GetComponent(entity);

                if (_containers.IsEntityInContainer(entity, xform)) continue;

                var pressureMovements = EnsureComp<MovedByPressureComponent>(entity);
                if (pressure.LastHighPressureMovementAirCycle < gridAtmosphere.UpdateCounter)
                {
                    // tl;dr YEET
                    ExperiencePressureDifference(
                        pressureMovements,
                        gridAtmosphere.UpdateCounter,
                        tile.PressureDifference,
                        tile.PressureDirection, 0,
                        tile.PressureSpecificTarget?.GridIndices.ToEntityCoordinates(tile.GridIndex, _mapManager) ?? EntityCoordinates.Invalid,
                        xform,
                        body);
                }

            }

            if (tile.PressureDifference > 100)
            {
                // TODO ATMOS Do space wind graphics here!
            }

            if (_spaceWindSoundCooldown++ > SpaceWindSoundCooldownCycles)
                _spaceWindSoundCooldown = 0;
        }

        private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, TileAtmosphere other, float difference)
        {
            gridAtmosphere.HighPressureDelta.Add(tile);
            if (difference > tile.PressureDifference)
            {
                tile.PressureDifference = difference;
                tile.PressureDirection = (tile.GridIndices - other.GridIndices).GetDir().ToAtmosDirection();
            }
        }

        public void ExperiencePressureDifference(
            MovedByPressureComponent component,
            int cycle,
            float pressureDifference,
            AtmosDirection direction,
            float pressureResistanceProbDelta,
            EntityCoordinates throwTarget,
            TransformComponent? xform = null,
            PhysicsComponent? physics = null)
        {
            if (!Resolve(component.Owner, ref physics, false))
                return;

            if (!Resolve(component.Owner, ref xform)) return;

            // TODO ATMOS stuns?

            var maxForce = MathF.Sqrt(pressureDifference) * 2.25f;
            var moveProb = 100f;

            if (component.PressureResistance > 0)
                moveProb = MathF.Abs((pressureDifference / component.PressureResistance * MovedByPressureComponent.ProbabilityBasePercent) -
                                     MovedByPressureComponent.ProbabilityOffset);

            // Can we yeet the thing (due to probability, strength, etc.)
            if (moveProb > MovedByPressureComponent.ProbabilityOffset && _robustRandom.Prob(MathF.Min(moveProb / 100f, 1f))
                                                                      && !float.IsPositiveInfinity(component.MoveResist)
                                                                      && (physics.BodyType != BodyType.Static
                                                                          && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForcePushRatio)))
                || (physics.BodyType == BodyType.Static && (maxForce >= (component.MoveResist * MovedByPressureComponent.MoveForceForcePushRatio))))
            {
                if (HasComp<MobStateComponent>(physics.Owner))
                {
                    AddMobMovedByPressure(component, physics);
                }

                if (maxForce > MovedByPressureComponent.ThrowForce)
                {
                    // TODO: Technically these directions won't be correct but uhh I'm just here for optimisations buddy not to fix my old bugs.
                    if (throwTarget != EntityCoordinates.Invalid)
                    {
                        var moveForce = maxForce * MathHelper.Clamp(moveProb, 0, 100) / 15f;
                        var pos = ((throwTarget.Position - xform.Coordinates.Position).Normalized + direction.ToDirection().ToVec()).Normalized;
                        physics.ApplyLinearImpulse(pos * moveForce);
                    }

                    else
                    {
                        var moveForce = MathF.Min(maxForce * MathHelper.Clamp(moveProb, 0, 100) / 2500f, 20f);
                        physics.ApplyLinearImpulse(direction.ToDirection().ToVec() * moveForce);
                    }

                    component.LastHighPressureMovementAirCycle = cycle;
                }
            }
        }
    }
}
