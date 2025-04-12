using Content.Server.Stealth;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Adds a temporary invisibility effect
/// </summary>
[UsedImplicitly]
public sealed partial class AddStealth : EntityEffect
{
    [DataField]
    public float Time = 1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-add-stealth", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var amt = Time;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            amt *= (float)reagentArgs.Scale;
        }

        args.EntityManager.EntitySysManager.GetEntitySystem<StealthSystem>()
            .AddTemporaryStealth(args.TargetEntity, TimeSpan.FromSeconds(amt));
    }
}
