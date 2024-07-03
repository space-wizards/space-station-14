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

    public override void Effect(EntityEffectBaseArgs args)
    {
        
        var cleanseRate = CleanseRate;

        var bloodstreamSys = args.EntityManager.System<BloodstreamSystem>();

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Source == null || reagentArgs.Reagent == null)
                return;

            cleanseRate *= reagentArgs.Scale.Float();
            bloodstreamSys.FlushChemicals(args.TargetEntity, reagentArgs.Reagent.ID, cleanseRate);
        }
        else
        {
            bloodstreamSys.FlushChemicals(args.TargetEntity, "", cleanseRate);
        }
    }
}
