using System;
using Content.Server.Animals.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Animals.Systems
{
    /// <summary>
    ///     Gives ability to living beings with acceptable hunger level to produce reagents.
    /// </summary>
    //TODO: Actually it can produce any kind of reagent, so it could be renamed to something more generic...?
    internal class UdderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Update(float frameTime)
        {
            foreach (var (udder, hunger) in EntityManager.EntityQuery <UdderComponent, HungerComponent>(false))
            {
                hunger.HungerThresholds.TryGetValue(HungerThreshold.Peckish, out var tragetThreshhold);

                // Is there enough nutrition to produce reagent?
                if (hunger.CurrentHunger < tragetThreshhold)
                    continue;

                udder.AccumulatedFrameTime += frameTime;

                if (udder.AccumulatedFrameTime < udder.UpdateRate)
                    continue;

                if (!_solutionContainerSystem.TryGetSolution(udder.OwnerUid, udder.TargetSolutionName, out var solution))
                    continue;

                //TODO: toxins from bloodstream !?
                _solutionContainerSystem.TryAddReagent(udder.OwnerUid, solution, udder.ReagentId, udder.QuantityPerUpdate, out var accepted);
                udder.AccumulatedFrameTime = 0;
            }
        }
    }
}
