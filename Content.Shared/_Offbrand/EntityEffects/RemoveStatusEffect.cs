using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class RemoveStatusEffect : EntityEffectBase<RemoveStatusEffect>
{
    [DataField(required: true)]
    public EntProtoId EffectProto;

    /// <inheritdoc />
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString(
            "entity-effect-guidebook-status-effect-remove",
            ("chance", Probability),
            ("key", prototype.Index(EffectProto).Name));
}

public sealed class RemoveStatusEffectEntityEffectSystem : EntityEffectSystem<StatusEffectContainerComponent, RemoveStatusEffect>
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    protected override void Effect(Entity<StatusEffectContainerComponent> ent, ref EntityEffectEvent<RemoveStatusEffect> args)
    {
        _statusEffects.TryRemoveStatusEffect(ent, args.Effect.EffectProto);
    }
}
