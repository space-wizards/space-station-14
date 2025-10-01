using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class RemoveStatusEffect : EntityEffect
{
    [DataField(required: true)]
    public EntProtoId EffectProto;

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<StatusEffectsSystem>()
            .TryRemoveStatusEffect(args.TargetEntity, EffectProto);
    }

    /// <inheritdoc />
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString(
            "reagent-effect-guidebook-status-effect-remove",
            ("chance", Probability),
            ("key", prototype.Index(EffectProto).Name));
}
