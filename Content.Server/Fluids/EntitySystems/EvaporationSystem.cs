using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class EvaporationSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

        }

        public void CheckEvaporate(EntityUid ownerUid, Solution solution)
        {
            if (solution.CurrentVolume == 0)
            {
                EntityManager.QueueDeleteEntity(ownerUid);
            }
        }

        public void Evaporate(EntityUid ownerUid, Solution solution)
        {
            _solutionContainerSystem.SplitSolution(ownerUid, solution,
                ReagentUnit.Min(ReagentUnit.New(1), solution.CurrentVolume));

            if (solution.CurrentVolume == 0)
            {
                EntityManager.QueueDeleteEntity(ownerUid);
            }
            else
            {
                //TODO Raise event
                //UpdateStatus();
            }
        }

        // public void UpdateStatus(PuddleComponent puddleComponent)
        // {
        //     if (puddleComponent.Owner.Deleted) return;
        //
        //     // UpdateAppearance();
        //     UpdateSlip(puddleComponent);
        //
        //     // if (EvaporateThreshold == ReagentUnit.New(-1) || CurrentVolume > EvaporateThreshold)
        //     // {
        //     //     return;
        //     // }
        //     //
        //     // _evaporationToken = new CancellationTokenSource();
        //     //
        //     // // KYS to evaporate
        //     // Owner.SpawnTimer(TimeSpan.FromSeconds(EvaporateTime), Evaporate, _evaporationToken.Token);
        // }
    }
}
