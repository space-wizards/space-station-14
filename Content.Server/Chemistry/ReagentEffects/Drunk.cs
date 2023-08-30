using Content.Shared.Chemistry.Reagent;
using Content.Shared.Drunk;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class Drunk : ReagentEffect
{
    /// <summary>
    ///     BoozePower is how long each metabolism cycle will make the drunk effect last for.
    /// </summary>
    [DataField("boozePower")]
    public float BoozePower = 3f;

    /// <summary>
    ///     Whether speech should be slurred.
    /// </summary>
    [DataField("slurSpeech")]
    public bool SlurSpeech = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-drunk", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        var boozePower = BoozePower;

        boozePower *= args.Scale;

        var drunkSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedDrunkSystem>();
        drunkSys.TryApplyDrunkenness(args.SolutionEntity, boozePower, SlurSpeech);
    }
}
