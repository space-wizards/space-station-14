using System.Threading;
using Content.Server.Radio.Components;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Jittering;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CleanseCult : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cleanse-cultist", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        if (!entityManager.TryGetComponent(uid, out CosmicCultComponent? _))
        {
            return;
        }
        entityManager.EnsureComponent<CleanseCultComponent>(uid); // We just slap them with the component and let the Deconversion system handle the rest.
    }
}
