using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Changes status effects on entities: Adds, removes or sets time.
/// </summary>
[UsedImplicitly]
public sealed partial class ModifyStatusEffect : EntityEffect
{
    [DataField(required: true)]
    public EntProtoId EffectProto;

    /// <summary>
    /// Time for which status effect should be applied. Behaviour changes according to <see cref="Refresh" />.
    /// </summary>
    [DataField]
    public float Time = 2.0f;

    /// <remarks>
    /// true - refresh status effect time (update to greater value), false - accumulate status effect time.
    /// </remarks>
    [DataField]
    public bool Refresh = true;

    /// <summary>
    /// Should this effect add the status effect, remove time from it, or set its cooldown?
    /// </summary>
    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Add;

    /// <inheritdoc />
    public override void Effect(EntityEffectBaseArgs args)
    {
        var statusSys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();

        var time = Time;
        if (args is EntityEffectReagentArgs reagentArgs)
            time *= reagentArgs.Scale.Float();

        var duration = TimeSpan.FromSeconds(time);
        switch (Type)
        {
            case StatusEffectMetabolismType.Add:
                if (Refresh)
                    statusSys.TryUpdateStatusEffectDuration(args.TargetEntity, EffectProto, duration);
                else
                    statusSys.TryAddStatusEffectDuration(args.TargetEntity, EffectProto, duration);
                break;
            case StatusEffectMetabolismType.Remove:
                statusSys.TryAddTime(args.TargetEntity, EffectProto, -duration);
                break;
            case StatusEffectMetabolismType.Set:
                statusSys.TrySetStatusEffectDuration(args.TargetEntity, EffectProto, duration);
                break;
        }
    }

    /// <inheritdoc />
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(
            "reagent-effect-guidebook-status-effect",
            ("chance", Probability),
            ("type", Type),
            ("time", Time),
            ("key", prototype.Index(EffectProto).Name)
        );
}
