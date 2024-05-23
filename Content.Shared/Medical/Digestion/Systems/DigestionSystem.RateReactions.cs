using Content.Shared.Chemistry.Components;
using Content.Shared.Medical.Digestion.Components;

namespace Content.Shared.Medical.Digestion.Systems;

public sealed partial class DigestionSystem
{

    private void UpdateReactions(Entity<DigestionComponent> digester,  Entity<SolutionComponent> solution)
    {
        RunDigestionReactions(digester, solution);
    }

    private void UpdateCachedPrototypes(Entity<DigestionComponent> digester)
    {
        foreach (var digestionTypeId in digester.Comp.SupportedDigestionTypes)
        {
            var digestionType = _protoManager.Index(digestionTypeId);
            foreach (var digestionReactionId in digestionType.DigestionReactions)
            {
                var digestionReaction = _protoManager.Index(digestionReactionId);
                digester.Comp.CachedDigestionReactions.Add(digestionReaction.Data);
            }
            digester.Comp.CachedDigestionReactions.Sort();
        }
    }

    private void RunDigestionReactions(Entity<DigestionComponent> digester, Entity<SolutionComponent> solution)
    {
        if (solution.Comp.Solution.Volume == 0)
            return;
        foreach (var reaction in digester.Comp.CachedDigestionReactions)
        {
            var reactionRate = _rateReactionSystem.GetReactionRate(solution, reaction, digester.Comp.LastUpdate);
            if (reactionRate == 0)
                continue;
            _rateReactionSystem.RunReaction(digester, solution, reaction, digester.Comp.LastUpdate);
        }
    }
}
