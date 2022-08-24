using Content.Shared.Chemistry.Reagent;
using Content.Shared.Drunk;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class Drunk : ReagentEffect
{
    /// <summary>
    ///     BoozePower is how long each metabolism cycle will make the drunk effect last for.
    /// </summary>
    [DataField("boozePower")]
    public float BoozePower = 2f;

    /// <summary>
    ///     Whether speech should be slurred.
    /// </summary>
    [DataField("slurSpeech")]
    public bool SlurSpeech = true;

    public override void Effect(ReagentEffectArgs args)
    {
        var drunkSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedDrunkSystem>();
        drunkSys.TryApplyDrunkenness(args.SolutionEntity, BoozePower, SlurSpeech);
    }
}
