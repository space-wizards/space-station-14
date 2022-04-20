using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Audio;

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
            foreach (var drain in EntityQuery<DrainComponent>())
            {
                drain.Accumulator += frameTime;
                if (drain.Accumulator < drain.DrainFrequency)
                {
                    continue;
                }
                drain.Accumulator -= drain.DrainFrequency;

                /// Best to do this one every second rather than once every tick...
                _solutionSystem.TryGetSolution(drain.Owner, DrainComponent.SolutionName, out var drainSolution);

                if (drainSolution is null)
                    return;

                /// Remove a bit from the buffer
                _solutionSystem.SplitSolution(drain.Owner, drainSolution, (drain.UnitsDestroyedPerSecond * drain.DrainFrequency));

                /// This will ensure that UnitsPerSecond is per second...
                var amount = drain.UnitsPerSecond * drain.DrainFrequency;
                var xform = Transform(drain.Owner);
                List<PuddleComponent> puddles = new();

                foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, drain.Range))
                {
                    /// No InRangeUnobstructed because there's no collision group that fits right now
                    /// and these are placed by mappers and not buildable/movable so shouldnt really be a problem...
                    if (TryComp<PuddleComponent>(entity, out var puddleComp))
                    {
                        puddles.Add(puddleComp);
                    }
                }

                if (puddles.Count == 0)
                {
                    _ambientSoundSystem.SetAmbience(drain.Owner, false);
                    continue;
                }

                _ambientSoundSystem.SetAmbience(drain.Owner, true);

                amount /= puddles.Count;

                foreach (var puddle in puddles)
                {
                    /// Queue the solution deletion if it's empty. EvaporationSystem might also do this
                    /// but queuedelete should be pretty safe.
                    if (!_solutionSystem.TryGetSolution(puddle.Owner, puddle.SolutionName, out var puddleSolution))
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                        continue;
                    }

                    /// Removes the lowest of:
                    /// the drain component's units per second adjusted for # of puddles
                    /// the puddle's remaining volume (making it cleanly zero)
                    /// the drain's remaining volume in its buffer.
                    var transferSolution = _solutionSystem.SplitSolution(puddle.Owner, puddleSolution,
                        FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.CurrentVolume, drainSolution.AvailableVolume));

                    _solutionSystem.TryAddSolution(drain.Owner, drainSolution, transferSolution);

                    if (puddleSolution.CurrentVolume <= 0)
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                    }
                }
            }
        }
    }
}
