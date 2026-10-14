using Content.Shared.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class RemoveAllGeneticMutationsSystem : EntityEffectSystem<GeneticMutationComponent, RemoveAllGeneticMutations>
{
    [Dependency] private SharedGeneticsSystem _genetics = default!;

    protected override void Effect(Entity<GeneticMutationComponent> entity, ref EntityEffectEvent<RemoveAllGeneticMutations> args)
    {
        var mutationsToRemove = new List<string>(entity.Comp.ActiveMutations);
        
        foreach (var mutationId in mutationsToRemove)
        {
            _genetics.TryRemoveMutation(entity, mutationId);
        }
    }
}

public sealed partial class RemoveAllGeneticMutations : EntityEffectBase<RemoveAllGeneticMutations>
{
}
