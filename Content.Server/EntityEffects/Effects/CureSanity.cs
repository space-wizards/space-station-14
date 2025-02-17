// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CureSanity : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cure-sanity", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;

        if (!entityManager.TryGetComponent<SanityComponent>(args.TargetEntity, out var sanityComponent))
            return;

        args.EntityManager.System<SharedSanitySystem>().TryAddSanityLvl(args.TargetEntity, 100);
    }
}
