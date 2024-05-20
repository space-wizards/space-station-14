using Content.Server.Body.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Basically smoke and foam reactions.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemCleanBloodstream : EntityEffect
{
    [DataField]
    public float CleanseRate = 3.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-clean-bloodstream", ("chance", Probability));

    public override void Effect(EntityEffectArgs args)
    {
        if (args.Source == null || args.Reagent == null)
            return;

        var cleanseRate = CleanseRate;

        cleanseRate *= args.Scale;

        var bloodstreamSys = args.EntityManager.System<BloodstreamSystem>();
        bloodstreamSys.FlushChemicals(args.TargetEntity, args.Reagent.ID, cleanseRate);
    }
}
