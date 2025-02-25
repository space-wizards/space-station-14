using Content.Server.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems
{
    public sealed class StomachSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        public const string DefaultSolutionName = "stomach";

        public override void Initialize()
        {
            SubscribeLocalEvent<StomachComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<StomachComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<StomachComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<StomachComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        }

        private void OnMapInit(Entity<StomachComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
        }

        private void OnComponentInit(Entity<StomachComponent> ent, ref ComponentInit args)
        {
            int digestionResolution = (int)(ent.Comp.DigestionDelay / ent.Comp.UpdateInterval);

            for (int i = 0; i < digestionResolution; i++)
            {
                ent.Comp.DigestionSolutions.Add(new Solution());
            }
        }

        private void OnUnpaused(Entity<StomachComponent> ent, ref EntityUnpausedEvent args)
        {
            ent.Comp.NextUpdate += args.PausedTime;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<StomachComponent, OrganComponent, SolutionContainerManagerComponent>();
            while (query.MoveNext(out var uid, out var stomach, out var organ, out var sol))
            {
                if (_gameTiming.CurTime < stomach.NextUpdate)
                    continue;

                stomach.NextUpdate += stomach.UpdateInterval;

                // Get our solutions
                if (!_solutionContainerSystem.ResolveSolution((uid, sol), DefaultSolutionName, ref stomach.StomachSolution, out var stomachSolution))
                    continue;

                if (organ.Body is not { } body || !_solutionContainerSystem.TryGetSolution(body, stomach.BodySolutionName, out var bodySolution))
                    continue;

                TryComp<ReactionMixerComponent>(uid, out var reactionMixer);
                _solutionContainerSystem.UpdateChemicals(stomach.StomachSolution.Value, true, reactionMixer);

                var digestionResolution = stomach.DigestionSolutions.Count;

                for (var i = digestionResolution - 1; i >= -1; i--)
                {
                    Solution currentSolution = stomach.StomachSolution.Value.Comp.Solution;

                    if (i != -1)
                        currentSolution = stomach.DigestionSolutions[i];

                    Solution transferSolution = currentSolution.SplitSolution(stomach.DigestionDelay.TotalSeconds / digestionResolution);
                    var nextSolution = bodySolution.Value.Comp.Solution;

                    if (i + 1 < digestionResolution)
                        nextSolution = stomach.DigestionSolutions[i + 1];

                    nextSolution.AddSolution(transferSolution, _protoMan);
                }
            }
        }

        private void OnApplyMetabolicMultiplier(
            Entity<StomachComponent> ent,
            ref ApplyMetabolicMultiplierEvent args)
        {
            if (args.Apply)
            {
                ent.Comp.UpdateInterval *= args.Multiplier;
                return;
            }

            // This way we don't have to worry about it breaking if the stasis bed component is destroyed
            ent.Comp.UpdateInterval /= args.Multiplier;
        }

        public bool CanTransferSolution(
            EntityUid uid,
            Solution solution,
            StomachComponent? stomach = null,
            SolutionContainerManagerComponent? solutions = null)
        {
            return Resolve(uid, ref stomach, ref solutions, logMissing: false)
                && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.StomachSolution, out var stomachSolution)
                // TODO: For now no partial transfers. Potentially change by design
                && stomachSolution.CanAddSolution(solution);
        }

        public bool TryTransferSolution(
            EntityUid uid,
            Solution solution,
            StomachComponent? stomach = null,
            SolutionContainerManagerComponent? solutions = null)
        {
            if (!Resolve(uid, ref stomach, ref solutions, logMissing: false)
                || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.StomachSolution)
                || !CanTransferSolution(uid, solution, stomach, solutions))
            {
                return false;
            }

            _solutionContainerSystem.TryAddSolution(stomach.StomachSolution.Value, solution);

            return true;
        }
    }
}
