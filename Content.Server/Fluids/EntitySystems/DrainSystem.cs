using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.Fluids.EntitySystems
{
    public sealed class DrainSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
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
                    continue;
                }

                amount /= puddles.Count;

                foreach (var puddle in puddles)
                {
                    /// Queue the solution deletion if it's empty. EvaporationSystem might also do this
                    /// but queuedelete should be pretty safe.
                    if (!_solutionSystem.TryGetSolution(puddle.Owner, puddle.SolutionName, out var solution))
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                        continue;
                    }

                    /// Removes the adjusted equilvalent of DrainComponent.UnitsPerSecond, or just cleanly
                    /// removes everything if the current volume is less than that.
                    _solutionSystem.SplitSolution(puddle.Owner, solution,
                        FixedPoint2.Min(FixedPoint2.New(amount), solution.CurrentVolume));

                    if (solution.CurrentVolume <= 0)
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                    }
                }
            }
        }
    }
}
