using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Changes the knockdown timer on an entity or causes knockdown.
/// </summary>
[UsedImplicitly]
public sealed partial class ModifyKnockdown : EntityEffect
{
    /// <summary>
    /// Should we only affect those with crawler component? Note if this is false, it will paralyze non-crawler's instead.
    /// </summary>
    [DataField]
    public bool Crawling;

    /// <summary>
    /// Should we drop items when we fall?
    /// </summary>
    [DataField]
    public bool Drop;

    /// <summary>
    /// Time for which knockdown should be applied. Behaviour changes according to <see cref="StatusEffectMetabolismType"/>.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Should this effect add the status effect, remove time from it, or set its cooldown?
    /// </summary>
    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Add;

    /// <summary>
    /// Should this effect add knockdown?, remove time from it?, or set its cooldown?
    /// </summary>
    [DataField]
    public bool Refresh = true;

    /// <inheritdoc />
    public override void Effect(EntityEffectBaseArgs args)
    {
        var stunSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedStunSystem>();

        var time = Time;
        if (args is EntityEffectReagentArgs reagentArgs)
            time *= reagentArgs.Scale.Float();

        switch (Type)
        {
            case StatusEffectMetabolismType.Update:
                if (Crawling)
                {
                    stunSys.TryCrawling(args.TargetEntity, time, drop: Drop);
                }
                else
                {
                    stunSys.TryKnockdown(args.TargetEntity, time, drop: Drop);
                }
                break;
            case StatusEffectMetabolismType.Add:
                if (Crawling)
                {
                    stunSys.TryCrawling(args.TargetEntity, time, false, drop: Drop);
                }
                else
                {
                    stunSys.TryKnockdown(args.TargetEntity, time, false, drop: Drop);
                }
                break;
            case StatusEffectMetabolismType.Remove:
                    stunSys.AddKnockdownTime(args.TargetEntity, -time);
                break;
            case StatusEffectMetabolismType.Set:
                if (Crawling)
                {
                    stunSys.TryCrawling(args.TargetEntity, time, drop: Drop);
                }
                else
                {
                    stunSys.TryKnockdown(args.TargetEntity, time, drop: Drop);
                }
                stunSys.SetKnockdownTime(args.TargetEntity, time);
                break;
        }
    }

    /// <inheritdoc />
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(
            "reagent-effect-guidebook-knockdown",
            ("chance", Probability),
            ("type", Type),
            ("time", Time.TotalSeconds)
        );
}
