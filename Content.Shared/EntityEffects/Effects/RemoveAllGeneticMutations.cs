using Content.Shared.Genetics;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class RemoveAllGeneticMutationsSystem : EntityEffectSystem<GeneticMutationComponent, RemoveAllGeneticMutations>
{
    [Dependency] private SharedGeneticsSystem _genetics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<GeneticMutationComponent> entity, ref EntityEffectEvent<RemoveAllGeneticMutations> args)
    {
        if (!entity.Comp.MutadoneWarningShown)
        {
            _popup.PopupEntity(Loc.GetString("genetics-mutadone-warning"), entity.Owner, entity.Owner);
            entity.Comp.MutadoneWarningShown = true;
        }

        entity.Comp.MutadoneRecoveryActive = true;

        foreach (var mutationId in entity.Comp.ActiveMutations.ToArray())
            _genetics.TryRemoveMutation(entity, mutationId);

        entity.Comp.TemporaryMutationTimers.Clear();
        Dirty(entity.Owner, entity.Comp);
    }
}

public sealed partial class RemoveAllGeneticMutations : EntityEffectBase<RemoveAllGeneticMutations>
{
}
