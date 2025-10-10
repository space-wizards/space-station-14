using Content.Shared.Chemistry.Reagent;
using Content.Shared.Jittering;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
///     Adds the jitter status effect to a mob.
///     This doesn't use generic status effects because it needs to
///     take in some parameters that JitterSystem needs.
/// </summary>
public sealed partial class Jitter : EntityEffect
{
    [DataField]
    public float Amplitude = 10.0f;

    [DataField]
    public float Frequency = 4.0f;

    [DataField]
    public float Time = 2.0f;

    /// <remarks>
    ///     true - refresh jitter time,  false - accumulate jitter time
    /// </remarks>
    [DataField]
    public bool Refresh = true;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var time = Time;
        if (args is EntityEffectReagentArgs reagentArgs)
            time *= reagentArgs.Scale.Float();

        args.EntityManager.EntitySysManager.GetEntitySystem<SharedJitteringSystem>()
            .DoJitter(args.TargetEntity, TimeSpan.FromSeconds(time), Refresh, Amplitude, Frequency);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-jittering", ("chance", Probability));
}
