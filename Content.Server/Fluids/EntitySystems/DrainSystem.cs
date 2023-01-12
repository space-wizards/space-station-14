using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Audio;
using Robust.Shared.Collections;

namespace Content.Server.Fluids.EntitySystems
{
    public sealed class DrainSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var managerQuery = GetEntityQuery<SolutionContainerManagerComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var puddleQuery = GetEntityQuery<PuddleComponent>();
            var puddles = new ValueList<(EntityUid Entity, string Solution)>();

            foreach (var drain in EntityQuery<DrainComponent>())
            {
                drain.Accumulator += frameTime;
                if (drain.Accumulator < drain.DrainFrequency)
                {
                    continue;
                }
                drain.Accumulator -= drain.DrainFrequency;

                if (!managerQuery.TryGetComponent(drain.Owner, out var manager))
                    continue;

                // Best to do this one every second rather than once every tick...
                _solutionSystem.TryGetSolution(drain.Owner, DrainComponent.SolutionName, out var drainSolution, manager);

                if (drainSolution is null)
                    continue;

                // Remove a bit from the buffer
                _solutionSystem.SplitSolution(drain.Owner, drainSolution, (drain.UnitsDestroyedPerSecond * drain.DrainFrequency));

                // This will ensure that UnitsPerSecond is per second...
                var amount = drain.UnitsPerSecond * drain.DrainFrequency;

                if (!xformQuery.TryGetComponent(drain.Owner, out var xform))
                    continue;

                puddles.Clear();

                foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, drain.Range))
                {
                    // No InRangeUnobstructed because there's no collision group that fits right now
                    // and these are placed by mappers and not buildable/movable so shouldnt really be a problem...
                    if (puddleQuery.TryGetComponent(entity, out var puddle))
                    {
                        puddles.Add((entity, puddle.SolutionName));
                    }
                }

                if (puddles.Count == 0)
                {
                    _ambientSoundSystem.SetAmbience(drain.Owner, false);
                    continue;
                }

                _ambientSoundSystem.SetAmbience(drain.Owner, true);

                amount /= puddles.Count;

                foreach (var (puddle, solution) in puddles)
                {
                    // Queue the solution deletion if it's empty. EvaporationSystem might also do this
                    // but queuedelete should be pretty safe.
                    if (!_solutionSystem.TryGetSolution(puddle, solution, out var puddleSolution))
                    {
                        EntityManager.QueueDeleteEntity(puddle);
                        continue;
                    }

                    // Removes the lowest of:
                    // the drain component's units per second adjusted for # of puddles
                    // the puddle's remaining volume (making it cleanly zero)
                    // the drain's remaining volume in its buffer.
                    var transferSolution = _solutionSystem.SplitSolution(puddle, puddleSolution,
                        FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.Volume, drainSolution.AvailableVolume));

                    _solutionSystem.TryAddSolution(drain.Owner, drainSolution, transferSolution);

                    if (puddleSolution.Volume <= 0)
                    {
                        QueueDel(puddle);
                    }
                }
            }
        }
    }
}
